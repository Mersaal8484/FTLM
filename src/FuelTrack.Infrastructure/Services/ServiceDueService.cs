using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class ServiceDueService : IServiceDueService
{
    private readonly FuelTrackDbContext _db;

    public ServiceDueService(FuelTrackDbContext db)
    {
        _db = db;
    }

    public async Task<Result<GeneratorMaintenanceTableDto>> GetGeneratorMaintenanceTableAsync(
        int generatorId,
        CancellationToken ct = default)
    {
        var generator = await _db.Generators
            .AsNoTracking()
            .Include(g => g.SubStation)
            .FirstOrDefaultAsync(g => g.Id == generatorId, ct);

        if (generator is null)
        {
            return Result<GeneratorMaintenanceTableDto>.NotFound($"Generator with ID {generatorId} not found.");
        }

        var table = await BuildMaintenanceTableAsync(generator, ct);
        return Result<GeneratorMaintenanceTableDto>.Success(table);
    }

    public async Task<IReadOnlyList<GeneratorMaintenanceTableDto>> GetStationMaintenanceTablesAsync(
        int? subStationId = null,
        CancellationToken ct = default)
    {
        var query = _db.Generators
            .AsNoTracking()
            .Include(g => g.SubStation)
            .AsQueryable();

        if (subStationId.HasValue)
        {
            query = query.Where(g => g.SubStationId == subStationId.Value);
        }

        var generators = await query
            .OrderBy(g => g.SubStation.Name)
            .ThenBy(g => g.Name)
            .ToListAsync(ct);

        var tables = new List<GeneratorMaintenanceTableDto>();
        foreach (var generator in generators)
        {
            tables.Add(await BuildMaintenanceTableAsync(generator, ct));
        }

        return tables;
    }

    public async Task<DueServiceDashboardSummaryDto> GetDueDashboardSummaryAsync(
        int? subStationId = null,
        CancellationToken ct = default)
    {
        var tables = await GetStationMaintenanceTablesAsync(subStationId, ct);

        var totalDue = 0;
        var totalOverdue = 0;
        var generatorsWithDue = new List<GeneratorMaintenanceTableDto>();

        foreach (var table in tables)
        {
            var genDue = table.Items.Count(x => x.Status == ServiceDueStatus.Due);
            var genOverdue = table.Items.Count(x => x.Status == ServiceDueStatus.Overdue);

            totalDue += genDue;
            totalOverdue += genOverdue;

            if (table.DueOrOverdueCount > 0)
            {
                generatorsWithDue.Add(table);
            }
        }

        return new DueServiceDashboardSummaryDto(totalDue, totalOverdue, generatorsWithDue);
    }

    private async Task<GeneratorMaintenanceTableDto> BuildMaintenanceTableAsync(
        Generator generator,
        CancellationToken ct)
    {
        var normalizedKey = GeneratorModelKey.Normalize(generator.Model);

        // Fetch official closing running hours
        var latestOfficialClosing = await _db.MeterReadings
            .AsNoTracking()
            .Where(r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours &&
                        r.SubjectId == generator.Id &&
                        ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status) &&
                        r.Kind == MeterReadingKind.DailyClosing)
            .OrderByDescending(r => r.CapturedAtUtc)
            .FirstOrDefaultAsync(ct);

        // Fallback: if no daily closing, check any official meter reading for running hours
        var latestOfficialReading = latestOfficialClosing ?? await _db.MeterReadings
            .AsNoTracking()
            .Where(r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours &&
                        r.SubjectId == generator.Id &&
                        ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .OrderByDescending(r => r.CapturedAtUtc)
            .FirstOrDefaultAsync(ct);

        decimal? currentHours = latestOfficialReading is not null
            ? latestOfficialReading.RawValue * latestOfficialReading.MultiplierSnapshot
            : null;

        var lastClosingAtUtc = latestOfficialReading?.CapturedAtUtc;

        // Fetch definitions for this model
        var definitions = await _db.ServiceIntervalDefinitions
            .AsNoTracking()
            .Include(d => d.ServiceItem)
            .Where(d => d.GeneratorModelKey == normalizedKey)
            .OrderBy(d => d.ServiceItem.SortOrder)
            .ThenBy(d => d.ServiceItem.Code)
            .ToListAsync(ct);

        // If no definitions exist for this model, fallback to all active ServiceItems with suggested intervals
        var itemsDue = new List<GeneratorMaintenanceItemDueDto>();

        if (definitions.Count > 0)
        {
            foreach (var def in definitions)
            {
                itemsDue.Add(await CalculateItemDueAsync(generator.Id, def.ServiceItemId, def.ServiceItem.Code,
                    def.ServiceItem.NameAr, def.ServiceItem.NameEn, def.IntervalHours, def.Importance,
                    def.WarningLeadHours, currentHours, ct));
            }
        }
        else
        {
            // Fallback: show active service items using suggested intervals
            var activeItems = await _db.ServiceItems
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .ToListAsync(ct);

            foreach (var item in activeItems)
            {
                var interval = item.SuggestedIntervalHours ?? 250m;
                itemsDue.Add(await CalculateItemDueAsync(generator.Id, item.Id, item.Code,
                    item.NameAr, item.NameEn, interval, ServiceIntervalImportance.Normal,
                    null, currentHours, ct));
            }
        }

        var dueOrOverdueCount = itemsDue.Count(x => x.Status is ServiceDueStatus.Due or ServiceDueStatus.Overdue);

        return new GeneratorMaintenanceTableDto(
            generator.Id,
            generator.Name,
            generator.Model,
            normalizedKey,
            generator.SubStationId,
            generator.SubStation?.Name ?? string.Empty,
            currentHours,
            lastClosingAtUtc,
            itemsDue,
            dueOrOverdueCount);
    }

    private async Task<GeneratorMaintenanceItemDueDto> CalculateItemDueAsync(
        int generatorId,
        int serviceItemId,
        string itemCode,
        string nameAr,
        string nameEn,
        decimal intervalHours,
        ServiceIntervalImportance importance,
        decimal? customLeadHours,
        decimal? currentHours,
        CancellationToken ct)
    {
        var lastRecord = await _db.ServiceReplacementRecords
            .AsNoTracking()
            .Where(r => r.GeneratorId == generatorId && r.ServiceItemId == serviceItemId && r.VoidedAtUtc == null)
            .OrderByDescending(r => r.HoursAtService)
            .ThenByDescending(r => r.OccurredAtUtc)
            .FirstOrDefaultAsync(ct);

        var isMeterReset = false;
        if (lastRecord is not null)
        {
            isMeterReset = await _db.MeterReadings.AnyAsync(m =>
                m.SubjectType == MeterReadingSubjectType.GeneratorRunningHours &&
                m.SubjectId == generatorId &&
                m.IsMeterReset &&
                ReadingApprovalPolicy.OfficialStatuses.Contains(m.Status) &&
                m.CapturedAtUtc > lastRecord.OccurredAtUtc, ct);
        }

        var effectiveLead = ServiceDueCalculator.CalculateLeadHours(intervalHours, customLeadHours);

        if (!currentHours.HasValue)
        {
            return new GeneratorMaintenanceItemDueDto(
                serviceItemId,
                itemCode,
                nameAr,
                nameEn,
                intervalHours,
                importance,
                lastRecord?.HoursAtService,
                lastRecord?.OccurredAtUtc,
                lastRecord?.Kind,
                lastRecord?.Id,
                lastRecord != null ? lastRecord.HoursAtService + intervalHours : null,
                null,
                0m,
                ServiceDueStatus.NotBaselined,
                0m,
                effectiveLead,
                isMeterReset,
                "بانتظار قراءة ساعات رسمية",
                "Awaiting official hours reading");
        }

        var calc = ServiceDueCalculator.Calculate(
            lastRecord?.HoursAtService,
            intervalHours,
            currentHours.Value,
            customLeadHours,
            isMeterReset);

        var (labelAr, labelEn) = GetStatusLabels(calc);

        return new GeneratorMaintenanceItemDueDto(
            serviceItemId,
            itemCode,
            nameAr,
            nameEn,
            intervalHours,
            importance,
            lastRecord?.HoursAtService,
            lastRecord?.OccurredAtUtc,
            lastRecord?.Kind,
            lastRecord?.Id,
            calc.DueAtHours,
            calc.RemainingHours,
            calc.OverdueByHours,
            calc.Status,
            calc.ProgressFraction,
            calc.LeadHours,
            isMeterReset,
            labelAr,
            labelEn);
    }

    private static (string LabelAr, string LabelEn) GetStatusLabels(ServiceDueCalculationResult calc)
    {
        return calc.Status switch
        {
            ServiceDueStatus.Overdue => (
                $"متأخر {calc.OverdueByHours:0.##} ساعة",
                $"Overdue by {calc.OverdueByHours:0.##} hours"),
            ServiceDueStatus.Due => (
                "مستحق الآن",
                "Due now"),
            ServiceDueStatus.DueSoon => (
                $"متبقي {calc.RemainingHours:0.##} ساعة",
                $"Remaining {calc.RemainingHours:0.##} hours"),
            ServiceDueStatus.Ok => (
                $"متبقي {calc.RemainingHours:0.##} ساعة",
                $"Remaining {calc.RemainingHours:0.##} hours"),
            ServiceDueStatus.NotBaselined => (
                calc.Reason != null && calc.Reason.Contains("reset", StringComparison.OrdinalIgnoreCase)
                    ? "إعادة تأسيس مطلوبة (تصفير العداد)"
                    : "غير مؤسس",
                calc.Reason != null && calc.Reason.Contains("reset", StringComparison.OrdinalIgnoreCase)
                    ? "Rebaseline required (meter reset)"
                    : "Not baselined"),
            _ => ("غير محدد", "Unspecified")
        };
    }
}
