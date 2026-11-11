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
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;

    public ServiceDueService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, ICurrentUserService currentUser)
    {
        _db = db;
        _scopeFilter = scopeFilter;
        _currentUser = currentUser;
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

        if (!await HasAccessToSubStationAsync(generator.SubStationId))
            return Result<GeneratorMaintenanceTableDto>.Forbidden("You do not have access to this generator.");

        var table = await BuildMaintenanceTableAsync(generator, ct);
        return Result<GeneratorMaintenanceTableDto>.Success(table);
    }

    public async Task<IReadOnlyList<GeneratorMaintenanceTableDto>> GetStationMaintenanceTablesAsync(
        int? subStationId = null,
        CancellationToken ct = default)
    {
        var accessibleSubStationIds = await GetAccessibleSubStationIdsAsync();
        if (accessibleSubStationIds.Count == 0) return [];

        var query = _db.Generators
            .AsNoTracking()
            .Include(g => g.SubStation)
            .Where(g => accessibleSubStationIds.Contains(g.SubStationId))
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
        var totalDue = tables.Sum(table => table.Items.Count(item => item.Status == ServiceDueStatus.Due));
        var totalOverdue = tables.Sum(table => table.Items.Count(item => item.Status == ServiceDueStatus.Overdue));
        var generatorsWithDue = tables.Where(table => table.DueOrOverdueCount > 0).ToList();

        return new DueServiceDashboardSummaryDto(totalDue, totalOverdue, generatorsWithDue);
    }

    private async Task<GeneratorMaintenanceTableDto> BuildMaintenanceTableAsync(
        Generator generator,
        CancellationToken ct)
    {
        var normalizedKey = GeneratorModelKey.Normalize(generator.Model);

        // Fetch latest official daily closing meter reading for running hours
        var latestOfficialClosing = await _db.MeterReadings
            .AsNoTracking()
            .Where(r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours &&
                        r.SubjectId == generator.Id &&
                        r.Kind == MeterReadingKind.DailyClosing &&
                        ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .OrderByDescending(r => r.CapturedAtUtc)
            .FirstOrDefaultAsync(ct);

        // The due engine has a single official source: approved/recorded daily
        // closing running-hours readings. Intra-day or unrelated meter readings
        // must not move a service clock.
        var latestOfficialReading = latestOfficialClosing;

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

        var defMap = definitions.ToDictionary(d => d.ServiceItemId);
        var isModelBlank = string.IsNullOrWhiteSpace(normalizedKey);

        // Suggested intervals only prefill configuration forms. Return a row for
        // EVERY active item. Distinguish "model not set" from "no definition" (C5).
        var activeItems = await _db.ServiceItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Code)
            .ToListAsync(ct);

        var itemsDue = new List<GeneratorMaintenanceItemDueDto>();

        foreach (var item in activeItems)
        {
            if (!isModelBlank && defMap.TryGetValue(item.Id, out var def))
            {
                var due = await CalculateItemDueAsync(generator.Id, def.ServiceItemId, def.ServiceItem.Code,
                    def.ServiceItem.NameAr, def.ServiceItem.NameEn, def.IntervalHours, def.Importance,
                    def.WarningLeadHours, currentHours, ct);
                itemsDue.Add(due with { DefinitionLink = $"/maintenance/service/definitions?modelKey={Uri.EscapeDataString(normalizedKey)}" });
            }
            else
            {
                // Unconfigured item: check if there's any recorded service/baseline history (W3 helper)
                var previousRecord = await ServiceRecordHelper.GetLatestActiveRecordAsync(_db, generator.Id, item.Id, ct);
                var labelAr = isModelBlank ? "الموديل غير محدد" : "فترة الصيانة غير مهيأة";
                var labelEn = isModelBlank ? "Generator model not set" : "Interval not configured";
                var link = isModelBlank
                    ? "/maintenance/service/definitions"
                    : $"/maintenance/service/definitions?modelKey={Uri.EscapeDataString(normalizedKey)}";

                itemsDue.Add(new GeneratorMaintenanceItemDueDto(
                    item.Id,
                    item.Code,
                    item.NameAr,
                    item.NameEn,
                    0m,
                    ServiceIntervalImportance.Normal,
                    previousRecord?.HoursAtService,
                    previousRecord?.OccurredAtUtc,
                    previousRecord?.Kind,
                    previousRecord?.Id,
                    null,
                    null,
                    0m,
                    ServiceDueStatus.NotConfigured,
                    0m,
                    0m,
                    false,
                    labelAr,
                    labelEn,
                    link));
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
        var lastRecord = await ServiceRecordHelper.GetLatestActiveRecordAsync(_db, generatorId, serviceItemId, ct);
        var isMeterReset = await ServiceRecordHelper.HasMeterResetAfterAsync(_db, generatorId, lastRecord?.OccurredAtUtc, ct);

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
                "بانتظار قراءة ساعات التشغيل الرسمية",
                "Awaiting official running hours reading");
        }

        var calc = ServiceDueCalculator.Calculate(
            lastRecord?.HoursAtService,
            intervalHours,
            currentHours.Value,
            effectiveLead,
            isMeterReset);

        var (labelAr, labelEn) = FormatStatusLabel(calc);

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
            effectiveLead,
            isMeterReset,
            labelAr,
            labelEn);
    }

    private static (string Ar, string En) FormatStatusLabel(ServiceDueCalculationResult calc)
    {
        return calc.Status switch
        {
            ServiceDueStatus.Overdue => ($"متأخر {calc.OverdueByHours:0.##} ساعة", $"Overdue by {calc.OverdueByHours:0.##} h"),
            ServiceDueStatus.Due => ("مستحق الآن", "Due now"),
            ServiceDueStatus.DueSoon => ($"متبقي {calc.RemainingHours:0.##} ساعة", $"{calc.RemainingHours:0.##} h remaining"),
            ServiceDueStatus.Ok => ("سليم", "OK"),
            ServiceDueStatus.NotBaselined => ("غير مؤسس (يلزم تسجيل خط أساس أو إعادة تأسيس بعد تصفير العداد)", "Not baselined"),
            ServiceDueStatus.NotConfigured => ("غير مهيأ", "Not configured"),
            _ => ("غير محدد", "Unspecified")
        };
    }

    private async Task<List<int>> GetAccessibleSubStationIdsAsync()
    {
        if (_currentUser.HasAnyAdminRole)
            return await _db.SubStations.AsNoTracking().Select(x => x.Id).ToListAsync();
        return _currentUser.UserId is int userId
            ? await _scopeFilter.GetAccessibleSubStationIdsAsync(userId)
            : [];
    }

    private async Task<bool> HasAccessToSubStationAsync(int subStationId)
    {
        if (_currentUser.HasAnyAdminRole) return true;
        return _currentUser.UserId is int userId &&
            await _scopeFilter.HasAccessToSubStationAsync(userId, subStationId);
    }
}
