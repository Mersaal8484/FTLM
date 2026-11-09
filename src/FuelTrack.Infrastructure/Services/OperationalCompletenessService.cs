using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

/// <summary>
/// Computes non-blocking operational quality indicators. It does not manufacture
/// readings: missing slots remain visible to supervisors and are never persisted.
/// </summary>
public sealed class OperationalCompletenessService(
    FuelTrackDbContext db,
    IUserScopeFilterService scopeFilter)
{
    private const decimal AverageLoadTolerancePercent = 15m;
    private sealed record DailyClosingValue(MeterReadingSubjectType SubjectType, DateOnly BusinessDate, decimal Value);

    public async Task<Result<OperationalCompletenessDto>> GetAsync(
        int generatorId, DateOnly businessDate, int userId, CancellationToken ct = default)
    {
        var accessible = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);
        var generator = await db.Generators.AsNoTracking()
            .Where(g => g.Id == generatorId && accessible.Contains(g.SubStationId))
            .Select(g => new { g.Id }).SingleOrDefaultAsync(ct);
        if (generator is null)
            return Result<OperationalCompletenessDto>.NotFound("Generator not found or outside your scope.");

        var rows = await db.OperationalGeneratorReadings.AsNoTracking()
            .Where(r => r.GeneratorId == generatorId && r.BusinessDate == businessDate
                && ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .Select(r => new { r.CapturedAtUtc, r.ActiveLoadKw })
            .ToListAsync(ct);
        var capturedHours = rows.Select(r => r.CapturedAtUtc.Hour).Distinct().ToHashSet();
        var missingHours = Enumerable.Range(0, 24).Where(hour => !capturedHours.Contains(hour)).ToList();
        var meanActive = rows.Where(r => r.ActiveLoadKw.HasValue).Select(r => r.ActiveLoadKw!.Value).DefaultIfEmpty().Average();
        decimal? meanActiveLoad = rows.Any(r => r.ActiveLoadKw.HasValue) ? meanActive : null;

        // Daily closings provide the only official energy/hours deltas used for average load.
        var closings = await db.MeterReadings.AsNoTracking()
            .Where(r => r.SubjectId == generatorId && r.Kind == MeterReadingKind.DailyClosing
                && (r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced || r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours)
                && r.BusinessDate <= businessDate && ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .OrderBy(r => r.SubjectType).ThenBy(r => r.BusinessDate).ThenBy(r => r.CapturedAtUtc)
            .Select(r => new DailyClosingValue(r.SubjectType, r.BusinessDate, r.RawValue * r.MultiplierSnapshot))
            .ToListAsync(ct);
        decimal? energyDelta = DeltaFor(MeterReadingSubjectType.GeneratorEnergyProduced, closings, businessDate);
        decimal? hoursDelta = DeltaFor(MeterReadingSubjectType.GeneratorRunningHours, closings, businessDate);
        decimal? averageLoad = energyDelta.HasValue && hoursDelta is > 0 ? energyDelta.Value / hoursDelta.Value : null;
        var warning = averageLoad.HasValue && meanActiveLoad.HasValue && averageLoad.Value > 0
            && Math.Abs(averageLoad.Value - meanActiveLoad.Value) / averageLoad.Value * 100m > AverageLoadTolerancePercent;
        var hasDailyClosing = closings.Any(c => c.BusinessDate == businessDate);

        return Result<OperationalCompletenessDto>.Success(new(
            generatorId, businessDate, averageLoad, meanActiveLoad, warning, missingHours, !hasDailyClosing));
    }

    private static decimal? DeltaFor(MeterReadingSubjectType subject, IEnumerable<DailyClosingValue> closings, DateOnly date)
    {
        var values = closings.Where(c => c.SubjectType == subject).ToList();
        var current = values.LastOrDefault(c => c.BusinessDate == date);
        if (current is null) return null;
        var previous = values.LastOrDefault(c => c.BusinessDate < date);
        return previous is null || current.Value < previous.Value ? null : current.Value - previous.Value;
    }
}
