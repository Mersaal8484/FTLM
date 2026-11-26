using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

/// <summary>
/// Resolves read-only baseline previous readings for operational capture subjects
/// when no prior non-voided MeterReading submission exists, using linked Controller
/// state first and initialized Meter values second.
/// </summary>
internal sealed class PreviousReadingBaselineResolver
{
    internal sealed record FeederBaselineInfo(int Id, int? MeterId, decimal MeterMultiplier);

    internal sealed record ControllerBaselineInfo(
        int Id,
        int GeneratorId,
        double EnergyReading,
        double OperatingHours,
        string? EnergyUnit,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    internal sealed record MeterBaselineInfo(
        int Id,
        int? GeneratorId,
        double? EnergyReading,
        double? HoursReading,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    internal sealed record BaselineData(
        IReadOnlyDictionary<int, ControllerBaselineInfo> ControllersByGeneratorId,
        IReadOnlyDictionary<int, MeterBaselineInfo> MetersByGeneratorId,
        IReadOnlyDictionary<int, FeederBaselineInfo> FeedersById,
        IReadOnlyDictionary<int, MeterBaselineInfo> MetersById);

    public async Task<BaselineData> LoadAsync(
        FuelTrackDbContext db,
        int subStationId,
        IReadOnlyList<int> generatorIds,
        IReadOnlyList<FeederBaselineInfo> feeders,
        IReadOnlyList<int> solarMeterIds,
        CancellationToken cancellationToken)
    {
        var controllers = generatorIds.Count == 0
            ? []
            : await db.Controllers.AsNoTracking()
                .Where(c => c.DeletedAt == null && c.GeneratorId.HasValue && generatorIds.Contains(c.GeneratorId.Value))
                .OrderByDescending(c => c.UpdatedAt)
                .ThenByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Select(c => new ControllerBaselineInfo(
                    c.Id,
                    c.GeneratorId!.Value,
                    c.EnergyReading,
                    c.OperatingHours,
                    c.EnergyUnit,
                    c.CreatedAt,
                    c.UpdatedAt))
                .ToListAsync(cancellationToken);

        var controllersByGeneratorId = controllers
            .GroupBy(c => c.GeneratorId)
            .ToDictionary(g => g.Key, g => g.First());

        var feederMeterIds = feeders
            .Where(f => f.MeterId.HasValue)
            .Select(f => f.MeterId!.Value)
            .Distinct()
            .ToList();

        var meters = (generatorIds.Count == 0 && feederMeterIds.Count == 0 && solarMeterIds.Count == 0)
            ? []
            : await db.Meters.AsNoTracking()
                .Where(m => m.DeletedAt == null && m.IsActive &&
                            (m.SubStationId == null || m.SubStationId == subStationId) &&
                            ((m.GeneratorId.HasValue && generatorIds.Contains(m.GeneratorId.Value)) ||
                             feederMeterIds.Contains(m.Id) ||
                             solarMeterIds.Contains(m.Id)))
                .OrderByDescending(m => m.UpdatedAt)
                .ThenByDescending(m => m.CreatedAt)
                .ThenByDescending(m => m.Id)
                .Select(m => new MeterBaselineInfo(
                    m.Id,
                    m.GeneratorId,
                    m.EnergyReading,
                    m.HoursReading,
                    m.CreatedAt,
                    m.UpdatedAt))
                .ToListAsync(cancellationToken);

        var metersByGeneratorId = meters
            .Where(m => m.GeneratorId.HasValue)
            .GroupBy(m => m.GeneratorId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var metersById = meters.ToDictionary(m => m.Id);
        var feedersById = feeders.ToDictionary(f => f.Id);

        return new BaselineData(controllersByGeneratorId, metersByGeneratorId, feedersById, metersById);
    }

    public PreviousMeterReadingDto ResolveSubject(
        MeterReadingSubjectType subjectType,
        int subjectId,
        IReadOnlyList<MeterReading> submittedSubjectReadings,
        BaselineData baselines)
    {
        if (submittedSubjectReadings.Count > 0)
        {
            var any = submittedSubjectReadings[0];
            var official = submittedSubjectReadings.FirstOrDefault(
                x => x.Kind == MeterReadingKind.DailyClosing && ReadingApprovalPolicy.IsOfficial(x.Status));
            var chronological = submittedSubjectReadings.OrderBy(x => x.CapturedAtUtc).ThenBy(x => x.Id).ToList();
            var deltas = chronological.Zip(chronological.Skip(1), (previous, current) => new { previous, current })
                .Where(pair => !pair.current.IsMeterReset && pair.current.RawValue >= pair.previous.RawValue)
                .Select(pair => (pair.current.RawValue - pair.previous.RawValue) * pair.current.MultiplierSnapshot)
                .TakeLast(6)
                .ToList();

            return new PreviousMeterReadingDto(
                subjectType,
                subjectId,
                ToSubmittedDto(any),
                official is null ? null : ToSubmittedDto(official),
                deltas.Count == 0 ? null : deltas.Average());
        }

        var baseline = TryResolveBaseline(subjectType, subjectId, baselines);
        return new PreviousMeterReadingDto(
            subjectType,
            subjectId,
            baseline,
            baseline,
            null);
    }

    private static LatestMeterReadingDto? TryResolveBaseline(
        MeterReadingSubjectType subjectType,
        int subjectId,
        BaselineData baselines)
    {
        // 1. Controller screen baseline (generator energy & running hours only)
        if (subjectType is MeterReadingSubjectType.GeneratorEnergyProduced or MeterReadingSubjectType.GeneratorRunningHours &&
            baselines.ControllersByGeneratorId.TryGetValue(subjectId, out var ctrl))
        {
            var neverUpdated = ctrl.UpdatedAt <= ctrl.CreatedAt;
            var capturedAt = EnsureUtc(ctrl.UpdatedAt != default ? ctrl.UpdatedAt : ctrl.CreatedAt);

            if (subjectType == MeterReadingSubjectType.GeneratorEnergyProduced)
            {
                var isKwh = string.IsNullOrWhiteSpace(ctrl.EnergyUnit)
                    || string.Equals(ctrl.EnergyUnit.Trim(), "kWh", StringComparison.OrdinalIgnoreCase);

                if (isKwh && ctrl.EnergyReading >= 0d && !(ctrl.EnergyReading == 0d && neverUpdated))
                {
                    return CreateBaselineDto((decimal)ctrl.EnergyReading, 1m, capturedAt, "Controller");
                }
            }
            else if (ctrl.OperatingHours >= 0d && !(ctrl.OperatingHours == 0d && neverUpdated))
            {
                return CreateBaselineDto((decimal)ctrl.OperatingHours, 1m, capturedAt, "Controller");
            }
        }

        // 2. Meter initialization baseline
        if (subjectType is MeterReadingSubjectType.GeneratorEnergyProduced or MeterReadingSubjectType.GeneratorRunningHours &&
            baselines.MetersByGeneratorId.TryGetValue(subjectId, out var genMeter))
        {
            var raw = subjectType == MeterReadingSubjectType.GeneratorEnergyProduced
                ? genMeter.EnergyReading
                : genMeter.HoursReading;

            if (raw is >= 0d)
            {
                var capturedAt = EnsureUtc(genMeter.UpdatedAt != default ? genMeter.UpdatedAt : genMeter.CreatedAt);
                return CreateBaselineDto((decimal)raw.Value, 1m, capturedAt, "MeterInitial");
            }
        }

        if (subjectType == MeterReadingSubjectType.FeederEnergySent &&
            baselines.FeedersById.TryGetValue(subjectId, out var feeder) &&
            feeder.MeterId is int feederMeterId &&
            baselines.MetersById.TryGetValue(feederMeterId, out var feederMeter) &&
            feederMeter.EnergyReading is >= 0d)
        {
            var capturedAt = EnsureUtc(feederMeter.UpdatedAt != default ? feederMeter.UpdatedAt : feederMeter.CreatedAt);
            return CreateBaselineDto((decimal)feederMeter.EnergyReading.Value, feeder.MeterMultiplier, capturedAt, "MeterInitial");
        }

        if (subjectType == MeterReadingSubjectType.SolarEnergyProduced &&
            baselines.MetersById.TryGetValue(subjectId, out var solarMeter) &&
            solarMeter.EnergyReading is >= 0d)
        {
            var capturedAt = EnsureUtc(solarMeter.UpdatedAt != default ? solarMeter.UpdatedAt : solarMeter.CreatedAt);
            return CreateBaselineDto((decimal)solarMeter.EnergyReading.Value, 1m, capturedAt, "MeterInitial");
        }

        return null;
    }

    private static LatestMeterReadingDto ToSubmittedDto(MeterReading reading) => new(
        reading.RawValue,
        reading.MultiplierSnapshot,
        reading.CapturedAtUtc,
        reading.Kind,
        reading.Status,
        reading.IsMeterReset,
        "Submitted");

    private static LatestMeterReadingDto CreateBaselineDto(decimal rawValue, decimal multiplier, DateTime capturedAtUtc, string source) => new(
        rawValue,
        multiplier,
        capturedAtUtc,
        MeterReadingKind.Periodic,
        OperationalReadingStatus.Approved,
        false,
        source);

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
