using System.Text.Json;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Notifications;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FuelTrack.Infrastructure.Services;

/// <summary>
/// Read-time fuel accounting and safety episode detection. It intentionally stores events,
/// not derived consumption totals, so late approved readings are reflected automatically.
/// </summary>
public sealed class FuelLoadSafetyService(
    FuelTrackDbContext db,
    IUserScopeFilterService scopeFilter,
    INotificationService notifications,
    ILogger<FuelLoadSafetyService>? logger = null) : IFuelLoadSafetyService
{
    public async Task<Result<FuelConsumptionReportDto>> GetConsumptionAsync(
        int generatorId, DateTime fromUtc, DateTime toUtc, int userId, CancellationToken ct = default)
    {
        if (fromUtc > toUtc) return Result<FuelConsumptionReportDto>.Failure("The start time must be before the end time.");
        var allowed = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);
        var generator = await db.Generators.AsNoTracking()
            .Where(g => g.Id == generatorId && allowed.Contains(g.SubStationId))
            .Select(g => new { g.Id, g.SubStationId }).SingleOrDefaultAsync(ct);
        if (generator is null) return Result<FuelConsumptionReportDto>.NotFound("Generator not found or outside your scope.");

        var tankIds = await db.FuelTanks.AsNoTracking().Where(t => t.GeneratorId == generatorId)
            .Select(t => t.Id).ToListAsync(ct);

        // Fetch official readings using the central policy set
        var events = await db.TankLevelReadings.AsNoTracking()
            .Where(r => tankIds.Contains(r.FuelTankId) && r.CapturedAtUtc <= toUtc
                        && ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .OrderBy(r => r.CapturedAtUtc).ThenBy(r => r.Id)
            .Select(r => new { r.FuelTankId, r.CapturedAtUtc, r.ComputedLitres, r.BeforeComputedLitres, r.EventType, r.DeadStockLitresSnapshot })
            .ToListAsync(ct);

        // Also fetch pending refills to distinguish between an unapproved refill and an unregistered refill
        var pendingRefills = await db.TankLevelReadings.AsNoTracking()
            .Where(r => tankIds.Contains(r.FuelTankId) && r.CapturedAtUtc <= toUtc
                        && r.Status == OperationalReadingStatus.Pending
                        && (r.EventType == TankEventType.Receipt || r.EventType == TankEventType.TransferIn))
            .Select(r => new { r.FuelTankId, r.CapturedAtUtc })
            .ToListAsync(ct);

        decimal consumption = 0, refill = 0;
        var warnings = new List<string>();
        foreach (var group in events.GroupBy(x => x.FuelTankId))
        {
            decimal? previousAfter = null;
            DateTime? previousEventTime = null;
            foreach (var current in group)
            {
                var before = current.BeforeComputedLitres ?? current.ComputedLitres;
                if (previousAfter.HasValue && current.CapturedAtUtc >= fromUtc)
                {
                    if (current.EventType == TankEventType.Level && current.ComputedLitres > previousAfter.Value)
                    {
                        var hasPendingRefill = pendingRefills.Any(p => p.FuelTankId == current.FuelTankId
                            && p.CapturedAtUtc >= (previousEventTime ?? DateTime.MinValue)
                            && p.CapturedAtUtc <= current.CapturedAtUtc);

                        if (hasPendingRefill)
                        {
                            warnings.Add("Refill awaiting approval; consumption excluded until approved.");
                        }
                        else
                        {
                            warnings.Add("Unregistered refill detected between routine level readings.");
                        }
                    }
                    else
                    {
                        var segment = previousAfter.Value - before;
                        if (segment > 0) consumption += segment;
                    }
                    if (current.EventType is TankEventType.Receipt or TankEventType.TransferIn)
                    {
                        var added = current.ComputedLitres - before;
                        if (added > 0) refill += added;
                    }
                }
                previousAfter = current.ComputedLitres;
                previousEventTime = current.CapturedAtUtc;

                if (current.CapturedAtUtc >= fromUtc && TankVolumeService.IsAtOrBelowDeadStock(current.ComputedLitres, current.DeadStockLitresSnapshot))
                    warnings.Add("Daily tank level is at or below dead stock.");
            }
        }

        var energyReadings = await db.MeterReadings.AsNoTracking()
            .Where(r => r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced && r.SubjectId == generatorId
                        && r.Kind == MeterReadingKind.DailyClosing && r.CapturedAtUtc <= toUtc
                        && ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .OrderBy(r => r.CapturedAtUtc).ThenBy(r => r.Id)
            .Select(r => new { r.CapturedAtUtc, Value = r.RawValue * r.MultiplierSnapshot })
            .ToListAsync(ct);

        decimal produced = 0;
        for (var i = 1; i < energyReadings.Count; i++)
            if (energyReadings[i].CapturedAtUtc >= fromUtc && energyReadings[i].Value >= energyReadings[i - 1].Value)
                produced += energyReadings[i].Value - energyReadings[i - 1].Value;

        return Result<FuelConsumptionReportDto>.Success(new(
            generatorId, fromUtc, toUtc, consumption, refill,
            energyReadings.Count > 1 ? produced : null,
            consumption > 0 && energyReadings.Count > 1 ? produced / consumption : null,
            warnings.Distinct().ToList()));
    }

    public async Task<Result<FuelReconciliationReportDto>> GetReconciliationAsync(
        int subStationId, DateTime fromUtc, DateTime toUtc, int userId, CancellationToken ct = default)
    {
        var allowed = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);
        if (!allowed.Contains(subStationId)) return Result<FuelReconciliationReportDto>.NotFound("Site not found or outside your scope.");

        var subStation = await db.SubStations.AsNoTracking().Where(s => s.Id == subStationId).Select(s => new { s.StationId }).FirstOrDefaultAsync(ct);
        var stationSettings = subStation != null
            ? await db.StationOperationalSettings.AsNoTracking().FirstOrDefaultAsync(s => s.StationId == subStation.StationId, ct)
            : null;
        var thresholdPct = stationSettings?.ProducedVsSentWarningThresholdPercent ?? 10m;

        var tanks = await db.FuelTanks.AsNoTracking().Where(t => t.SubStationId == subStationId)
            .Select(t => new { t.Id, t.Name, t.ParentTankId }).ToListAsync(ct);
        var readings = await db.TankLevelReadings.AsNoTracking()
            .Where(r => tanks.Select(t => t.Id).Contains(r.FuelTankId) && r.CapturedAtUtc >= fromUtc && r.CapturedAtUtc <= toUtc
                && ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .Select(r => new { r.FuelTankId, r.EventType, r.BeforeComputedLitres, r.ComputedLitres, r.DeliveryNoteLitres })
            .ToListAsync(ct);

        var lines = tanks.Select(t =>
        {
            var own = readings.Where(r => r.FuelTankId == t.Id).ToList();
            var transferOut = own.Where(r => r.EventType == TankEventType.TransferOut).Sum(r => Math.Max(0, (r.BeforeComputedLitres ?? r.ComputedLitres) - r.ComputedLitres));
            var transferIn = own.Where(r => r.EventType == TankEventType.TransferIn).Sum(r => Math.Max(0, r.ComputedLitres - (r.BeforeComputedLitres ?? r.ComputedLitres)));
            var receipts = own.Where(r => r.EventType == TankEventType.Receipt).Sum(r => r.DeliveryNoteLitres ?? Math.Max(0, r.ComputedLitres - (r.BeforeComputedLitres ?? r.ComputedLitres)));
            var measured = own.Where(r => r.EventType == TankEventType.Receipt).Sum(r => Math.Max(0, r.ComputedLitres - (r.BeforeComputedLitres ?? r.ComputedLitres)));
            var difference = receipts - measured;
            var warnings = new List<string>();
            if (receipts > 0 && Math.Abs(difference) / receipts > .10m) warnings.Add("Receipt differs from measured level by more than 10%.");
            return new FuelReconciliationLineDto(t.Id, t.Name, transferOut, transferIn, receipts, measured, difference, warnings);
        }).ToList();

        decimal totalMainTransferOut = 0;
        decimal totalDailyTransferIn = 0;
        foreach (var line in lines)
        {
            var tank = tanks.First(t => t.Id == line.TankId);
            if (tank.ParentTankId == null)
            {
                totalMainTransferOut += line.TransferOutLitres;
            }
            else
            {
                totalDailyTransferIn += line.TransferInLitres;
            }
        }

        var siteGenerators = await db.Generators.AsNoTracking().Where(g => g.SubStationId == subStationId).Select(g => g.Id).ToListAsync(ct);
        decimal totalGenConsumption = 0;
        foreach (var genId in siteGenerators)
        {
            var consResult = await GetConsumptionAsync(genId, fromUtc, toUtc, userId, ct);
            if (consResult.IsSuccess && consResult.Value != null)
            {
                totalGenConsumption += consResult.Value.ConsumptionLitres;
            }
        }

        var variance = Math.Abs(totalMainTransferOut - totalDailyTransferIn);
        var thresholdFlagged = totalMainTransferOut > 0 && (variance / totalMainTransferOut * 100m) > thresholdPct;

        return Result<FuelReconciliationReportDto>.Success(new(
            subStationId, fromUtc, toUtc,
            totalMainTransferOut, totalDailyTransferIn, totalGenConsumption,
            variance, thresholdFlagged, lines));
    }

    public async Task<IReadOnlyList<OverloadEventDto>> GetOverloadsAsync(int userId, int? subStationId, bool includeResolved, CancellationToken ct = default)
    {
        var allowed = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);
        return await db.OverloadEvents.AsNoTracking()
            .Where(e => allowed.Contains(e.Generator.SubStationId) && (!subStationId.HasValue || e.Generator.SubStationId == subStationId) && (includeResolved || e.ResolvedAtUtc == null))
            .OrderByDescending(e => e.StartedAtUtc)
            .Select(e => new OverloadEventDto(e.Id, e.GeneratorId, e.Generator.Name, e.TriggerReadingId, e.StartedAtUtc, e.ResolvedAtUtc, e.PeakPercent))
            .ToListAsync(ct);
    }

    public async Task<Result<IReadOnlyList<GeneratorOperatingLimitDto>>> GetLimitsAsync(int generatorId, int userId, CancellationToken ct = default)
    {
        if (!await CanAccessGeneratorAsync(generatorId, userId, ct)) return Result<IReadOnlyList<GeneratorOperatingLimitDto>>.NotFound("Generator not found or outside your scope.");
        var limits = await db.GeneratorOperatingLimits.AsNoTracking().Where(x => x.GeneratorId == generatorId)
            .Select(x => new GeneratorOperatingLimitDto(x.Id, x.GeneratorId, x.Parameter, x.Minimum, x.Maximum)).ToListAsync(ct);
        return Result<IReadOnlyList<GeneratorOperatingLimitDto>>.Success(limits);
    }

    public async Task<Result<GeneratorOperatingLimitDto>> SaveLimitAsync(int generatorId, SaveGeneratorOperatingLimitDto dto, int userId, CancellationToken ct = default)
    {
        if (dto.Minimum is null && dto.Maximum is null) return Result<GeneratorOperatingLimitDto>.Failure("At least one operating limit is required.");
        if (dto.Minimum.HasValue && dto.Maximum.HasValue && dto.Minimum > dto.Maximum) return Result<GeneratorOperatingLimitDto>.Failure("Minimum cannot exceed maximum.");
        if (!await CanAccessGeneratorAsync(generatorId, userId, ct)) return Result<GeneratorOperatingLimitDto>.NotFound("Generator not found or outside your scope.");
        var limit = await db.GeneratorOperatingLimits.SingleOrDefaultAsync(x => x.GeneratorId == generatorId && x.Parameter == dto.Parameter, ct);
        if (limit is null) { limit = new GeneratorOperatingLimit { GeneratorId = generatorId, Parameter = dto.Parameter, CreatedAt = DateTime.UtcNow, CreatedBy = userId }; db.GeneratorOperatingLimits.Add(limit); }
        limit.Minimum = dto.Minimum; limit.Maximum = dto.Maximum; limit.UpdatedAt = DateTime.UtcNow; limit.UpdatedBy = userId;
        await db.SaveChangesAsync(ct);
        return Result<GeneratorOperatingLimitDto>.Success(new(limit.Id, limit.GeneratorId, limit.Parameter, limit.Minimum, limit.Maximum));
    }

    public async Task<Result<OperationalDailyLogDto>> GetDailyLogAsync(int generatorId, DateOnly businessDate, int userId, CancellationToken ct = default)
    {
        if (!await CanAccessGeneratorAsync(generatorId, userId, ct)) return Result<OperationalDailyLogDto>.NotFound("Generator not found or outside your scope.");
        var rows = await db.OperationalGeneratorReadings.AsNoTracking()
            .Where(x => x.GeneratorId == generatorId && x.BusinessDate == businessDate && ReadingApprovalPolicy.OfficialStatuses.Contains(x.Status))
            .OrderBy(x => x.CapturedAtUtc)
            .Select(x => new
            {
                x.CapturedAtUtc,
                x.ActiveLoadKw,
                x.ApparentLoadKva,
                x.PowerFactor,
                x.FrequencyHz,
                x.LineVoltageVolts,
                x.CurrentAmps,
                x.OilPressure,
                x.WaterTemperatureCelsius,
                x.OilTemperatureCelsius,
                x.PeakLoadKw,
                x.WarningsJson
            })
            .ToListAsync(ct);

        var result = rows.Select(x => new OperationalTrendPointDto(
            x.CapturedAtUtc,
            x.ActiveLoadKw,
            x.ApparentLoadKva,
            x.PowerFactor,
            x.FrequencyHz,
            x.LineVoltageVolts,
            x.CurrentAmps,
            x.OilPressure,
            x.WaterTemperatureCelsius,
            x.OilTemperatureCelsius,
            x.PeakLoadKw,
            false,
            ParseWarnings(x.WarningsJson))).ToList();

        return Result<OperationalDailyLogDto>.Success(new(generatorId, businessDate, result));
    }

    public async Task EvaluateOnReceiptAsync(int generatorReadingId, CancellationToken ct = default)
    {
        var reading = await db.OperationalGeneratorReadings
            .Include(x => x.Generator)
            .ThenInclude(g => g.SubStation)
            .SingleOrDefaultAsync(x => x.Id == generatorReadingId, ct);

        if (reading is null || reading.Generator.ActualPowerKW <= 0) return;

        var stationId = reading.Generator.SubStation?.StationId ?? 0;
        var settings = stationId > 0
            ? await db.StationOperationalSettings.AsNoTracking().FirstOrDefaultAsync(s => s.StationId == stationId, ct)
            : null;

        var thresholdPercent = settings?.OverloadThresholdPercent ?? 100m;
        var hysteresis = settings?.OverloadHysteresisReadings ?? 3;

        var observed = new[] { reading.ActiveLoadKw, reading.PeakLoadKw }
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0m)
            .Max();

        await AddConsistencyWarningsAsync(reading, ct);

        var open = await db.OverloadEvents.SingleOrDefaultAsync(
            x => x.GeneratorId == reading.GeneratorId && x.ResolvedAtUtc == null, ct);

        // Ignore readings older than the latest evaluated reading
        if (open is not null && open.LatestReadingCapturedAtUtc.HasValue && reading.CapturedAtUtc < open.LatestReadingCapturedAtUtc.Value)
        {
            return;
        }

        var thresholdKw = reading.Generator.ActualPowerKW * (thresholdPercent / 100m);
        // Overload: observed > thresholdKw (strictly above; exactly 100% is NOT an overload)
        var isOverload = observed > thresholdKw;

        if (!isOverload)
        {
            if (open is not null)
            {
                open.ConsecutiveNormalReadings++;
                open.LatestReadingId = reading.Id;
                open.LatestReadingCapturedAtUtc = reading.CapturedAtUtc;
                open.UpdatedAt = DateTime.UtcNow;

                if (open.ConsecutiveNormalReadings >= hysteresis)
                {
                    // Stamp ResolvedAt with the reading's capture time
                    open.ResolvedAtUtc = reading.CapturedAtUtc;
                }
                await db.SaveChangesAsync(ct);
            }
            return;
        }

        var percent = observed / reading.Generator.ActualPowerKW * 100m;
        if (open is not null)
        {
            open.LatestReadingId = reading.Id;
            open.LatestReadingCapturedAtUtc = reading.CapturedAtUtc;
            open.PeakPercent = Math.Max(open.PeakPercent, percent);
            open.ConsecutiveNormalReadings = 0; // reset normal counter
            open.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return;
        }

        var entity = new OverloadEvent
        {
            GeneratorId = reading.GeneratorId,
            TriggerReadingId = reading.Id,
            LatestReadingId = reading.Id,
            StartedAtUtc = reading.CapturedAtUtc,
            LatestReadingCapturedAtUtc = reading.CapturedAtUtc,
            PeakPercent = percent,
            ConsecutiveNormalReadings = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.OverloadEvents.Add(entity);
        await db.SaveChangesAsync(ct);

        await PublishAsync(reading.Generator.SubStationId, "Operational overload",
            $"{reading.Generator.Name} exceeded its configured capacity ({percent:0.##}%).",
            "OverloadEvent", entity.Id, ct);
    }

    public async Task EvaluateOperatingLimitsAsync(CancellationToken ct = default)
    {
        var limits = await db.GeneratorOperatingLimits
            .Include(x => x.Generator)
            .ToListAsync(ct);

        if (limits.Count == 0) return;

        var genIds = limits.Select(l => l.GeneratorId).Distinct().ToList();
        var minWatermark = limits.Min(l => l.LastEvaluatedCapturedAtUtc) ?? DateTime.MinValue;

        // Batch query all new Tier-1 readings since stored watermark across limits
        var newReadings = await db.OperationalGeneratorReadings.AsNoTracking()
            .Where(r => genIds.Contains(r.GeneratorId)
                        && ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status)
                        && r.CapturedAtUtc > minWatermark)
            .OrderBy(r => r.CapturedAtUtc)
            .ToListAsync(ct);

        if (newReadings.Count == 0) return;

        var limitIds = limits.Select(l => l.Id).ToList();
        var openEpisodes = await db.OperatingLimitEpisodes
            .Where(e => limitIds.Contains(e.GeneratorOperatingLimitId) && e.ResolvedAtUtc == null)
            .ToListAsync(ct);

        var readingsByGen = newReadings.GroupBy(r => r.GeneratorId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var limit in limits)
        {
            if (!readingsByGen.TryGetValue(limit.GeneratorId, out var genReadings))
                continue;

            var watermark = limit.LastEvaluatedCapturedAtUtc ?? DateTime.MinValue;
            var applicable = genReadings.Where(r => r.CapturedAtUtc > watermark).OrderBy(r => r.CapturedAtUtc).ToList();
            if (applicable.Count == 0) continue;

            var open = openEpisodes.FirstOrDefault(e => e.GeneratorOperatingLimitId == limit.Id && e.ResolvedAtUtc == null);

            foreach (var reading in applicable)
            {
                var observed = ValueFor(limit.Parameter, reading);
                if (!observed.HasValue) continue;

                var breached = (limit.Minimum.HasValue && observed < limit.Minimum) ||
                               (limit.Maximum.HasValue && observed > limit.Maximum);

                if (!breached)
                {
                    if (open is not null)
                    {
                        open.ResolvedAtUtc = reading.CapturedAtUtc;
                        open.UpdatedAt = DateTime.UtcNow;
                        open = null;
                    }
                }
                else
                {
                    if (open is not null)
                    {
                        open.ObservedValue = observed.Value;
                        open.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        open = new OperatingLimitEpisode
                        {
                            GeneratorOperatingLimitId = limit.Id,
                            TriggerReadingId = reading.Id,
                            StartedAtUtc = reading.CapturedAtUtc,
                            ObservedValue = observed.Value,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        db.OperatingLimitEpisodes.Add(open);
                        await db.SaveChangesAsync(ct);

                        await PublishAsync(limit.Generator.SubStationId, "Operating limit exceeded",
                            $"{limit.Generator.Name}: {limit.Parameter} is outside its configured limit.",
                            "OperatingLimitEpisode", open.Id, ct);
                    }
                }
            }

            var last = applicable.Last();
            limit.LastEvaluatedCapturedAtUtc = last.CapturedAtUtc;
            limit.LastEvaluatedReadingId = last.Id;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task AddConsistencyWarningsAsync(OperationalGeneratorReading reading, CancellationToken ct)
    {
        var warnings = ParseWarnings(reading.WarningsJson).ToList();

        // 1) |kW - kVA * PF| / kVA > 10%
        if (reading.ActiveLoadKw.HasValue && reading.ApparentLoadKva.HasValue && reading.PowerFactor.HasValue && reading.ApparentLoadKva > 0)
        {
            var expected = reading.ApparentLoadKva.Value * reading.PowerFactor.Value;
            if (expected > 0 && Math.Abs(reading.ActiveLoadKw.Value - expected) / expected > .10m)
                warnings.Add("Active load differs from kVA × PF by more than 10%.");
        }

        // 2) Three-phase kVA ˜ v3 * V * A / 1000 (15%)
        if (reading.LineVoltageVolts.HasValue && reading.CurrentAmps.HasValue && reading.ApparentLoadKva.HasValue
            && reading.LineVoltageVolts > 0 && reading.CurrentAmps > 0 && reading.ApparentLoadKva > 0)
        {
            var v = (double)reading.LineVoltageVolts.Value;
            var a = (double)reading.CurrentAmps.Value;
            var expectedKva = (decimal)(Math.Sqrt(3.0) * v * a / 1000.0);
            if (expectedKva > 0 && Math.Abs(reading.ApparentLoadKva.Value - expectedKva) / expectedKva > 0.15m)
            {
                warnings.Add("Three-phase kVA differs from v3·V·A/1000 by more than 15%.");
            }
        }

        // 3) CONSECUTIVE repeated values (default 6)
        if (reading.ActiveLoadKw.HasValue)
        {
            var recentKw = await db.OperationalGeneratorReadings.AsNoTracking()
                .Where(x => x.GeneratorId == reading.GeneratorId && x.Id != reading.Id && x.CapturedAtUtc <= reading.CapturedAtUtc)
                .OrderByDescending(x => x.CapturedAtUtc)
                .Take(5)
                .Select(x => x.ActiveLoadKw)
                .ToListAsync(ct);

            if (recentKw.Count >= 5 && recentKw.All(kw => kw == reading.ActiveLoadKw))
            {
                warnings.Add("Repeated active-load value detected across 6 consecutive readings.");
            }
        }

        reading.WarningsJson = JsonSerializer.Serialize(warnings.Distinct());
        await db.SaveChangesAsync(ct);
    }

    private static decimal? ValueFor(GeneratorOperatingParameter p, OperationalGeneratorReading r) => p switch
    {
        GeneratorOperatingParameter.OilPressure => r.OilPressure,
        GeneratorOperatingParameter.WaterTemperature => r.WaterTemperatureCelsius,
        GeneratorOperatingParameter.OilTemperature => r.OilTemperatureCelsius,
        GeneratorOperatingParameter.Frequency => r.FrequencyHz,
        GeneratorOperatingParameter.Voltage => r.LineVoltageVolts,
        GeneratorOperatingParameter.PowerFactor => r.PowerFactor,
        GeneratorOperatingParameter.Current => r.CurrentAmps,
        _ => null
    };

    private async Task<bool> CanAccessGeneratorAsync(int generatorId, int userId, CancellationToken ct) =>
        (await scopeFilter.GetAccessibleSubStationIdsAsync(userId)).Contains(await db.Generators.Where(g => g.Id == generatorId).Select(g => g.SubStationId).FirstOrDefaultAsync(ct));

    private async Task PublishAsync(int subStationId, string title, string message, string type, int id, CancellationToken ct)
    {
        try
        {
            await notifications.PublishScopedAsync(new CreateNotificationDto(subStationId, title, message, type, "Warning", null, type, id), null, ["operational_data"], ct);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to publish notification {Type} {Id} for sub-station {SubStationId}", type, id, subStationId);
        }
    }

    private static IReadOnlyList<string> ParseWarnings(string? json)
    {
        try { return string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return []; }
    }
}