using FuelTrack.Application.DTOs.Notifications;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FuelTrack.Infrastructure.Services;

public sealed class ServiceDueEvaluationService : IServiceDueEvaluationService
{
    private const string MaintenanceModule = "maintenance";

    private readonly FuelTrackDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<ServiceDueEvaluationService> _logger;
    private readonly ICustomerFeatureService _customerFeatureService;

    public ServiceDueEvaluationService(
        FuelTrackDbContext db,
        INotificationService notifications,
        ILogger<ServiceDueEvaluationService> logger,
        ICustomerFeatureService? customerFeatureService = null)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
        _customerFeatureService = customerFeatureService ?? new CustomerFeatureService(db);
    }

    public async Task<int> EvaluateAllGeneratorsAsync(CancellationToken ct = default)
    {
        if (!await _customerFeatureService.IsOperationalDataNewModeAsync(ct)) return 0;

        var generatorIds = await _db.Generators
            .AsNoTracking()
            .Select(g => g.Id)
            .ToListAsync(ct);

        var totalAlerts = 0;
        foreach (var generatorId in generatorIds)
        {
            try
            {
                totalAlerts += await EvaluateGeneratorAsync(generatorId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to evaluate service due for generator {GeneratorId}", generatorId);
            }
        }

        return totalAlerts;
    }

    public async Task<int> EvaluateGeneratorAsync(int generatorId, CancellationToken ct = default)
    {
        if (!await _customerFeatureService.IsOperationalDataNewModeAsync(ct)) return 0;

        var generator = await _db.Generators
            .Include(g => g.SubStation)
            .FirstOrDefaultAsync(g => g.Id == generatorId, ct);

        if (generator is null)
        {
            return 0;
        }

        // Daily closing is the sole official source for service-clock evaluation.
        var latestOfficialClosing = await _db.MeterReadings
            .AsNoTracking()
            .Where(r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours &&
                        r.SubjectId == generator.Id &&
                        r.Kind == MeterReadingKind.DailyClosing &&
                        ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .OrderByDescending(r => r.CapturedAtUtc)
            .FirstOrDefaultAsync(ct);

        var latestOfficialReading = latestOfficialClosing;

        if (latestOfficialReading is null)
        {
            // No official running hours recorded yet
            return 0;
        }

        var currentHours = latestOfficialReading.RawValue * latestOfficialReading.MultiplierSnapshot;
        var modelKey = GeneratorModelKey.Normalize(generator.Model);

        var definitions = await _db.ServiceIntervalDefinitions
            .Include(d => d.ServiceItem)
            .Where(d => d.GeneratorModelKey == modelKey)
            .ToListAsync(ct);

        if (definitions.Count == 0)
        {
            return 0;
        }

        var alertsCreated = 0;

        foreach (var def in definitions)
        {
            var lastRecord = await ServiceRecordHelper.GetLatestActiveRecordAsync(_db, generator.Id, def.ServiceItemId, ct);
            var isMeterReset = await ServiceRecordHelper.HasMeterResetAfterAsync(_db, generator.Id, lastRecord?.OccurredAtUtc, ct);

            var calc = ServiceDueCalculator.Calculate(
                currentHours,
                lastRecord?.HoursAtService,
                def.IntervalHours,
                def.WarningLeadHours,
                isMeterReset);

            var state = await _db.ServiceDueStates
                .FirstOrDefaultAsync(s => s.GeneratorId == generator.Id && s.ServiceItemId == def.ServiceItemId, ct);

            var currentCycleId = lastRecord?.Id;

            if (state is null)
            {
                state = new ServiceDueState
                {
                    GeneratorId = generator.Id,
                    ServiceItemId = def.ServiceItemId,
                    CycleRecordId = currentCycleId,
                    HighestNotifiedStatus = null,
                    CurrentStatus = calc.Status,
                    RemainingHours = calc.RemainingHours,
                    OverdueByHours = calc.OverdueByHours,
                    DueAtHours = calc.DueAtHours,
                    LastEvaluatedAtUtc = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 0,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = 0
                };
                _db.ServiceDueStates.Add(state);
            }
            else
            {
                state.CurrentStatus = calc.Status;
                state.RemainingHours = calc.RemainingHours;
                state.OverdueByHours = calc.OverdueByHours;
                state.DueAtHours = calc.DueAtHours;
            }

            var shouldNotify = ServiceDueCalculator.ShouldNotify(
                state.HighestNotifiedStatus,
                calc.Status,
                state.CycleRecordId,
                currentCycleId);

            if (shouldNotify)
            {
                var (type, severity, msgAr, msgEn) = FormatAlert(
                    generator.Name,
                    def.ServiceItem.NameAr,
                    def.ServiceItem.NameEn,
                    def.Importance,
                    calc);

                // Persist the transition before publishing (W7b). Concurrency guard.
                state.CycleRecordId = currentCycleId;
                state.HighestNotifiedStatus = calc.Status;
                state.LastEvaluatedAtUtc = DateTime.UtcNow;
                state.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                // Message includes generator and item names and hours (C6)
                var dto = new CreateNotificationDto(
                    generator.SubStationId,
                    msgAr,
                    msgEn,
                    type,
                    severity,
                    null,
                    "Generator",
                    generator.Id);

                try
                {
                    await _notifications.PublishScopedAsync(dto, null, MaintenanceModule, ct);
                    alertsCreated++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Service due transition was committed but notification publish failed for generator {GeneratorId}", generator.Id);
                }
            }
            else if (currentCycleId != state.CycleRecordId && calc.Status == ServiceDueStatus.Ok)
            {
                // Cycle reset back to Ok: update cycle and clear alert status without notifying (B2)
                state.CycleRecordId = currentCycleId;
                state.HighestNotifiedStatus = null;
                state.LastEvaluatedAtUtc = DateTime.UtcNow;
                state.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                state.LastEvaluatedAtUtc = DateTime.UtcNow;
                state.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        return alertsCreated;
    }

    private static (string Type, string Severity, string MessageAr, string MessageEn) FormatAlert(
        string generatorName,
        string itemNameAr,
        string itemNameEn,
        ServiceIntervalImportance importance,
        ServiceDueCalculationResult calc)
    {
        // Severity derives from both definition Importance and Status (B5)
        var severity = (importance, calc.Status) switch
        {
            (ServiceIntervalImportance.Critical, ServiceDueStatus.Overdue) => "danger",
            (ServiceIntervalImportance.Critical, ServiceDueStatus.Due) => "danger",
            (ServiceIntervalImportance.Critical, ServiceDueStatus.DueSoon) => "warning",

            (ServiceIntervalImportance.High, ServiceDueStatus.Overdue) => "danger",
            (ServiceIntervalImportance.High, ServiceDueStatus.Due) => "warning",
            (ServiceIntervalImportance.High, ServiceDueStatus.DueSoon) => "info",

            _ => calc.Status == ServiceDueStatus.Overdue ? "warning" : "info"
        };

        var type = calc.Status switch
        {
            ServiceDueStatus.Overdue => "service_overdue",
            ServiceDueStatus.Due => "service_due",
            _ => "service_due_soon"
        };

        var msgAr = calc.Status switch
        {
            ServiceDueStatus.Overdue => $"خدمة {itemNameAr} للمولد {generatorName} متأخرة بمقدار {calc.OverdueByHours:0.##} ساعة تشغيل.",
            ServiceDueStatus.Due => $"خدمة {itemNameAr} للمولد {generatorName} مستحقة الآن.",
            _ => $"خدمة {itemNameAr} للمولد {generatorName} مستحقة قريباً (متبقي {calc.RemainingHours:0.##} ساعة تشغيل)."
        };

        var msgEn = calc.Status switch
        {
            ServiceDueStatus.Overdue => $"{itemNameEn} service on generator {generatorName} is overdue by {calc.OverdueByHours:0.##} operating hours.",
            ServiceDueStatus.Due => $"{itemNameEn} service on generator {generatorName} is due now.",
            _ => $"{itemNameEn} service on generator {generatorName} is due soon ({calc.RemainingHours:0.##} operating hours remaining)."
        };

        return (type, severity, msgAr, msgEn);
    }
}
