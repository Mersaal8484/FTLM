using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

/// <summary>
/// Phase 3: station energy balance per day from official (IsOfficial) data only.
/// Import = generator energy produced + solar import.
/// Export = feeder energy sent.
/// When internal consumption is not configured the threshold does NOT fire.
/// </summary>
public sealed class EnergyBalanceService(
    FuelTrackDbContext db,
    IMeterDeltaService meterDelta) : IEnergyBalanceService
{
    private const string UnexplainedLabel = "غير مفسَّر (يشمل الاستهلاك الداخلي)";

    public async Task<EnergyBalanceDto> GetStationBalanceAsync(
        int stationId, DateOnly date, CancellationToken cancellationToken = default)
    {
        // Station settings for threshold and optional internal meter
        var settings = await db.StationOperationalSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StationId == stationId, cancellationToken);

        decimal threshold = settings?.ProducedVsSentWarningThresholdPercent ?? 10m;
        int? internalMeterId = settings?.InternalConsumptionMeterId;

        // All SubStation IDs for this Station
        var subStationIds = await db.SubStations
            .AsNoTracking()
            .Where(s => s.StationId == stationId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        if (subStationIds.Count == 0)
            return ZeroBalance(stationId, date);

        // All approved MeterReading DailyClosing rows for this date in this station
        var closings = await db.MeterReadings
            .AsNoTracking()
            .Where(r => r.Kind == MeterReadingKind.DailyClosing
                        && r.BusinessDate == date
                        && ReadingApprovalPolicy.IsOfficial(r.Status)
                        && db.OperationalSubmissions
                            .Where(s => s.Id == r.OperationalSubmissionId)
                            .Any(s => subStationIds.Contains(s.SubStationId)))
            .ToListAsync(cancellationToken);

        // Helper: sum deltas for a subject type across all relevant subjects
        async Task<decimal> SumDeltasAsync(MeterReadingSubjectType subjectType, IEnumerable<int>? subjectIds = null)
        {
            var relevant = closings.Where(r => r.SubjectType == subjectType);
            if (subjectIds is not null)
                relevant = relevant.Where(r => subjectIds.Contains(r.SubjectId));

            decimal total = 0m;
            foreach (var r in relevant)
            {
                var d = await meterDelta.GetDailyDeltaAsync(subjectType, r.SubjectId, date, cancellationToken);
                if (d?.Delta is decimal val && val >= 0) total += val;
            }
            return total;
        }

        // Generator import: GeneratorEnergyProduced
        var genSubjectIds = closings
            .Where(r => r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced)
            .Select(r => r.SubjectId).Distinct().ToList();
        decimal generatorImport = await SumDeltasAsync(MeterReadingSubjectType.GeneratorEnergyProduced, genSubjectIds);

        // Solar import: SolarImport (if the enum value exists)
        decimal solarImport = 0m;
        bool hasSolarEnum = Enum.IsDefined(typeof(MeterReadingSubjectType), "SolarImport");
        if (hasSolarEnum)
        {
            var solarType = (MeterReadingSubjectType)Enum.Parse(typeof(MeterReadingSubjectType), "SolarImport");
            var solarSubjectIds = closings
                .Where(r => r.SubjectType == solarType)
                .Select(r => r.SubjectId).Distinct().ToList();
            solarImport = await SumDeltasAsync(solarType, solarSubjectIds);
        }

        decimal totalImport = generatorImport + solarImport;

        // Export: FeederEnergySent
        var feederSubjectIds = closings
            .Where(r => r.SubjectType == MeterReadingSubjectType.FeederEnergySent)
            .Select(r => r.SubjectId).Distinct().ToList();
        decimal exportKwh = await SumDeltasAsync(MeterReadingSubjectType.FeederEnergySent, feederSubjectIds);

        // Internal consumption
        decimal? internalKwh = null;
        if (internalMeterId.HasValue)
        {
            var internalReading = closings.FirstOrDefault(r => r.MeterId == internalMeterId.Value);
            if (internalReading is not null)
            {
                var d = await meterDelta.GetDailyDeltaAsync(internalReading.SubjectType, internalReading.SubjectId, date, cancellationToken);
                if (d?.Delta is decimal v && v >= 0) internalKwh = v;
            }
        }

        decimal unexplained = totalImport - exportKwh - (internalKwh ?? 0m);
        decimal unexplainedPct = totalImport > 0 ? unexplained / totalImport * 100m : 0m;
        bool thresholdExceeded = internalKwh.HasValue && Math.Abs(unexplainedPct) > threshold;
        decimal? solarShare = (hasSolarEnum && totalImport > 0) ? solarImport / totalImport * 100m : null;

        return new EnergyBalanceDto(
            stationId, date,
            totalImport, generatorImport, solarImport,
            exportKwh,
            internalKwh,
            IsInternalMeasured: internalKwh.HasValue,
            unexplained, unexplainedPct,
            solarShare,
            thresholdExceeded,
            internalKwh.HasValue ? string.Empty : UnexplainedLabel);
    }

    private static EnergyBalanceDto ZeroBalance(int stationId, DateOnly date) =>
        new(stationId, date, 0, 0, 0, 0, null, false, 0, 0, null, false, UnexplainedLabel);
}
