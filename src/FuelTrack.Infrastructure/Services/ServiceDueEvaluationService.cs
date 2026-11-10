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
    private static readonly string[] MaintenanceModule = ["maintenance"];
    private readonly FuelTrackDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<ServiceDueEvaluationService> _logger;

    public ServiceDueEvaluationService(
        FuelTrackDbContext db,
        INotificationService notifications,
        ILogger<ServiceDueEvaluationService> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<int> EvaluateAllGeneratorsAsync(CancellationToken ct = default)
    {
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
        var generator = await _db.Generators
            .Include(g => g.SubStation)
            .FirstOrDefaultAsync(g => g.Id == generatorId, ct);

        if (generator is null) return 0;

        // Fetch official closing running hours
        var latestOfficialClosing = await _db.MeterReadings
            .AsNoTracking()
            .Where(r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours &&
                        r.SubjectId == generator.Id &&
                        ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status) &&
                        r.Kind == MeterReadingKind.DailyClosing)
            .OrderByDescending(r => r.CapturedAtUtc)
            .FirstOrDefaultAsync(ct);

        var latestOfficialReading = latestOfficialClosing ?? await _db.MeterReadings
            .AsNoTracking()
            .Where(r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours &&
                        r.SubjectId == generator.Id &&
                        ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .OrderByDescending(r => r.CapturedAtUtc)
            .FirstOrDefaultAsync(ct);

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
            var lastRecord = await _db.ServiceReplacementRecords
                .Where(r => r.GeneratorId == generator.Id && r.ServiceItemId == def.ServiceItemId && r.VoidedAtUtc == null)
                .OrderByDescending(r => r.HoursAtService)
                .ThenByDescending(r => r.OccurredAtUtc)
                .FirstOrDefaultAsync(ct);

            var isMeterReset = false;
            if (lastRecord is not null)
            {
                isMeterReset = await _db.MeterReadings.AnyAsync(m =>
                    m.SubjectType == MeterReadingSubjectType.GeneratorRunningHours &&
                    m.SubjectId == generator.Id &&
                    m.IsMeterReset &&
                    ReadingApprovalPolicy.OfficialStatuses.Contains(m.Status) &&
                    m.CapturedAtUtc > lastRecord.OccurredAtUtc, ct);
            }

            var calc = ServiceDueCalculator.Calculate(
                lastRecord?.HoursAtService,
                def.IntervalHours,
                currentHours,
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
                    LastEvaluatedAtUtc = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 0,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = 0
                };
                _db.ServiceDueStates.Add(state);
            }

            var shouldNotify = ServiceDueCalculator.ShouldNotify(
                state.HighestNotifiedStatus,
                calc.Status,
                state.CycleRecordId,
                currentCycleId);

            if (shouldNotify)
            {
                var (type, severity, msgAr, msgEn) = FormatAlert(generator.Name, def.ServiceItem.NameAr, def.ServiceItem.NameEn, calc);

                var dto = new CreateNotificationDto(
                    generator.SubStationId,
                    "Service required",
                    msgEn,
                    type,
                    severity,
                    null,
                    "Generator",
                    generator.Id);

                await _notifications.PublishScopedAsync(dto, null, MaintenanceModule, ct);

                state.CycleRecordId = currentCycleId;
                state.HighestNotifiedStatus = calc.Status;
                state.LastEvaluatedAtUtc = DateTime.UtcNow;
                state.UpdatedAt = DateTime.UtcNow;
                alertsCreated++;
            }
            else if (currentCycleId != state.CycleRecordId && calc.Status == ServiceDueStatus.Ok)
            {
                // Cycle reset back to Ok: update cycle and clear alert status without notifying
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
        ServiceDueCalculationResult calc)
    {
        return calc.Status switch
        {
            ServiceDueStatus.Overdue => (
                "service_overdue",
                "danger",
                $"خدمة {itemNameAr} للمولد {generatorName} متأخرة بمقدار {calc.OverdueByHours:0.##} ساعة تشغيل.",
                $"{itemNameEn} service on generator {generatorName} is overdue by {calc.OverdueByHours:0.##} operating hours."),
            ServiceDueStatus.Due => (
                "service_due",
                "warning",
                $"خدمة {itemNameAr} للمولد {generatorName} مستحقة الآن.",
                $"{itemNameEn} service on generator {generatorName} is due now."),
            _ => (
                "service_due_soon",
                "warning",
                $"خدمة {itemNameAr} للمولد {generatorName} مستحقة قريباً (متبقي {calc.RemainingHours:0.##} ساعة تشغيل).",
                $"{itemNameEn} service on generator {generatorName} is due soon ({calc.RemainingHours:0.##} operating hours remaining).")
        };
    }
}
