using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
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
    private sealed record DailyClosingValue(MeterReadingSubjectType SubjectType, DateOnly BusinessDate, decimal Value);

    public async Task<Result<OperationalCompletenessDto>> GetAsync(
        int generatorId, DateOnly businessDate, int userId, CancellationToken ct = default)
    {
        var accessible = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);
        var generator = await db.Generators.AsNoTracking()
            .Where(g => g.Id == generatorId && accessible.Contains(g.SubStationId))
            .Select(g => new { g.Id, g.SubStation.StationId })
            .SingleOrDefaultAsync(ct);
        if (generator is null)
            return Result<OperationalCompletenessDto>.NotFound("Generator not found or outside your scope.");

        var policy = await GetPolicyAsync(generator.StationId, ct);
        var rows = await db.OperationalGeneratorReadings.AsNoTracking()
            .Where(r => r.GeneratorId == generatorId && r.BusinessDate == businessDate
                && ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .Select(r => new { r.CapturedAtUtc, r.ActiveLoadKw })
            .ToListAsync(ct);
        var missingSlots = FindMissingSlots(businessDate, rows.Select(r => r.CapturedAtUtc), policy);
        var activeLoads = rows.Where(r => r.ActiveLoadKw.HasValue).Select(r => r.ActiveLoadKw!.Value).ToList();
        decimal? meanActiveLoad = activeLoads.Count == 0 ? null : activeLoads.Average();

        // Daily closings provide the only official energy/hours deltas used for average load.
        var closings = await db.MeterReadings.AsNoTracking()
            .Where(r => r.SubjectId == generatorId && r.Kind == MeterReadingKind.DailyClosing
                && (r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced || r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours)
                && r.BusinessDate <= businessDate && ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .OrderBy(r => r.SubjectType).ThenBy(r => r.BusinessDate).ThenBy(r => r.CapturedAtUtc)
            .Select(r => new DailyClosingValue(r.SubjectType, r.BusinessDate, r.RawValue * r.MultiplierSnapshot))
            .ToListAsync(ct);
        var energyDelta = DeltaFor(MeterReadingSubjectType.GeneratorEnergyProduced, closings, businessDate);
        var hoursDelta = DeltaFor(MeterReadingSubjectType.GeneratorRunningHours, closings, businessDate);
        decimal? averageLoad = energyDelta.HasValue && hoursDelta is > 0 ? energyDelta.Value / hoursDelta.Value : null;
        var warning = averageLoad.HasValue && meanActiveLoad.HasValue && averageLoad.Value > 0
            && Math.Abs(averageLoad.Value - meanActiveLoad.Value) / averageLoad.Value * 100m > policy.AverageLoadTolerancePercent;
        var hasEnergyClosing = closings.Any(c => c.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced && c.BusinessDate == businessDate);
        var hasHoursClosing = closings.Any(c => c.SubjectType == MeterReadingSubjectType.GeneratorRunningHours && c.BusinessDate == businessDate);

        return Result<OperationalCompletenessDto>.Success(new(
            generatorId, businessDate, averageLoad, meanActiveLoad, warning, missingSlots, !hasEnergyClosing || !hasHoursClosing));
    }

    public async Task<Result<FeederOperationalCompletenessDto>> GetFeederAsync(
        int feederId, DateOnly businessDate, int userId, CancellationToken ct = default)
    {
        var accessible = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);
        var feeder = await db.Feeders.AsNoTracking()
            .Where(f => f.Id == feederId && accessible.Contains(f.SubStationId))
            .Select(f => new { f.Id, f.SubStation.StationId })
            .SingleOrDefaultAsync(ct);
        if (feeder is null)
            return Result<FeederOperationalCompletenessDto>.NotFound("Feeder not found or outside your scope.");

        var policy = await GetPolicyAsync(feeder.StationId, ct);
        var capturedAtUtc = await db.FeederLoadReadings.AsNoTracking()
            .Where(r => r.FeederId == feederId && r.BusinessDate == businessDate
                && ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .Select(r => r.CapturedAtUtc)
            .ToListAsync(ct);
        var hasDailyClosing = await db.MeterReadings.AsNoTracking().AnyAsync(r =>
            r.SubjectType == MeterReadingSubjectType.FeederEnergySent && r.SubjectId == feederId
            && r.Kind == MeterReadingKind.DailyClosing && r.BusinessDate == businessDate
            && ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status), ct);

        return Result<FeederOperationalCompletenessDto>.Success(new(
            feederId, businessDate, FindMissingSlots(businessDate, capturedAtUtc, policy), !hasDailyClosing));
    }

    private async Task<StationPolicy> GetPolicyAsync(int stationId, CancellationToken ct)
    {
        var settings = await db.StationOperationalSettings.AsNoTracking()
            .Where(s => s.StationId == stationId)
            .Select(s => new { s.TimeZone, s.MissingDataIntervalMinutes, s.AverageLoadWarningTolerancePercent })
            .SingleOrDefaultAsync(ct);

        var interval = settings?.MissingDataIntervalMinutes ?? StationOperationalSettings.DefaultMissingDataIntervalMinutes;
        if (interval is < 5 or > 1440 || 1440 % interval != 0)
            interval = StationOperationalSettings.DefaultMissingDataIntervalMinutes;
        return new StationPolicy(
            ResolveTimeZone(settings?.TimeZone),
            interval,
            settings?.AverageLoadWarningTolerancePercent ?? StationOperationalSettings.DefaultAverageLoadWarningTolerancePercent);
    }

    private static IReadOnlyList<string> FindMissingSlots(DateOnly businessDate, IEnumerable<DateTime> capturedAtUtc, StationPolicy policy)
    {
        var observed = capturedAtUtc
            .Select(value => ToSlot(DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(AsUtc(value), policy.TimeZone)), TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(AsUtc(value), policy.TimeZone)), policy.IntervalMinutes))
            .Where(slot => slot.Date == businessDate)
            .Select(slot => slot.Time)
            .ToHashSet();
        var slots = 1440 / policy.IntervalMinutes;
        return Enumerable.Range(0, slots)
            .Select(index => TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(index * policy.IntervalMinutes)))
            .Where(slot => !observed.Contains(slot))
            .Select(slot => slot.ToString("HH:mm"))
            .ToList();
    }

    private static (DateOnly Date, TimeOnly Time) ToSlot(DateOnly date, TimeOnly time, int intervalMinutes)
    {
        var minutes = time.Hour * 60 + time.Minute;
        return (date, TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes / intervalMinutes * intervalMinutes)));
    }

    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static TimeZoneInfo ResolveTimeZone(string? id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id ?? StationOperationalSettings.DefaultTimeZone); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }

    private static decimal? DeltaFor(MeterReadingSubjectType subject, IEnumerable<DailyClosingValue> closings, DateOnly date)
    {
        var values = closings.Where(c => c.SubjectType == subject).ToList();
        var current = values.LastOrDefault(c => c.BusinessDate == date);
        if (current is null) return null;
        var previous = values.LastOrDefault(c => c.BusinessDate < date);
        return previous is null || current.Value < previous.Value ? null : current.Value - previous.Value;
    }

    private sealed record StationPolicy(TimeZoneInfo TimeZone, int IntervalMinutes, decimal AverageLoadTolerancePercent);
}
