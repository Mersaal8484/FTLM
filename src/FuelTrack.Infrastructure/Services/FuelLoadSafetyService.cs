using System.Text.Json;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Notifications;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

/// <summary>
/// Read-time fuel accounting and safety episode detection. It intentionally stores events,
/// not derived consumption totals, so late approved readings are reflected automatically.
/// </summary>
public sealed class FuelLoadSafetyService(
    FuelTrackDbContext db,
    IUserScopeFilterService scopeFilter,
    INotificationService notifications) : IFuelLoadSafetyService
{
    private static bool Official(OperationalReadingStatus status) =>
        status is OperationalReadingStatus.Recorded or OperationalReadingStatus.Approved;

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
        var events = await db.TankLevelReadings.AsNoTracking()
            .Where(r => tankIds.Contains(r.FuelTankId) && r.CapturedAtUtc <= toUtc
                        && (r.Status == OperationalReadingStatus.Recorded || r.Status == OperationalReadingStatus.Approved))
            .OrderBy(r => r.CapturedAtUtc).ThenBy(r => r.Id)
            .Select(r => new { r.FuelTankId, r.CapturedAtUtc, r.ComputedLitres, r.BeforeComputedLitres, r.EventType, r.DeadStockLitresSnapshot })
            .ToListAsync(ct);

        decimal consumption = 0, refill = 0;
        var warnings = new List<string>();
        foreach (var group in events.GroupBy(x => x.FuelTankId))
        {
            decimal? previousAfter = null;
            foreach (var current in group)
            {
                // Events before the requested range establish the opening level only.
                var before = current.BeforeComputedLitres ?? current.ComputedLitres;
                if (previousAfter.HasValue && current.CapturedAtUtc >= fromUtc)
                {
                    if (current.EventType == TankEventType.Level && current.ComputedLitres > previousAfter.Value)
                    {
                        warnings.Add("Unregistered refill detected between routine level readings.");
                    }
                    else
                    {
                        // §7.9: consumption is the fall from the previous after-level to this before-level.
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
                if (current.CapturedAtUtc >= fromUtc && TankVolumeService.IsAtOrBelowDeadStock(current.ComputedLitres, current.DeadStockLitresSnapshot))
                    warnings.Add("Daily tank level is at or below dead stock.");
            }
        }

        var energyReadings = await db.MeterReadings.AsNoTracking()
            .Where(r => r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced && r.SubjectId == generatorId
                        && r.Kind == MeterReadingKind.DailyClosing && r.CapturedAtUtc <= toUtc
                        && (r.Status == OperationalReadingStatus.Recorded || r.Status == OperationalReadingStatus.Approved))
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
        var tanks = await db.FuelTanks.AsNoTracking().Where(t => t.SubStationId == subStationId)
            .Select(t => new { t.Id, t.Name, t.ParentTankId }).ToListAsync(ct);
        var readings = await db.TankLevelReadings.AsNoTracking()
            .Where(r => tanks.Select(t => t.Id).Contains(r.FuelTankId) && r.CapturedAtUtc >= fromUtc && r.CapturedAtUtc <= toUtc
                && (r.Status == OperationalReadingStatus.Recorded || r.Status == OperationalReadingStatus.Approved))
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
        return Result<FuelReconciliationReportDto>.Success(new(subStationId, fromUtc, toUtc, lines));
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
            .Where(x => x.GeneratorId == generatorId && x.BusinessDate == businessDate && (x.Status == OperationalReadingStatus.Recorded || x.Status == OperationalReadingStatus.Approved))
            .OrderBy(x => x.CapturedAtUtc).Select(x => new { x.CapturedAtUtc, x.ActiveLoadKw, x.ApparentLoadKva, x.PowerFactor, x.FrequencyHz, x.WarningsJson }).ToListAsync(ct);
        var result = rows.Select(x => new OperationalTrendPointDto(x.CapturedAtUtc, x.ActiveLoadKw, x.ApparentLoadKva, x.PowerFactor, x.FrequencyHz, false, ParseWarnings(x.WarningsJson))).ToList();
        return Result<OperationalDailyLogDto>.Success(new(generatorId, businessDate, result));
    }

    public async Task EvaluateOnReceiptAsync(int generatorReadingId, CancellationToken ct = default)
    {
        var reading = await db.OperationalGeneratorReadings.Include(x => x.Generator).SingleOrDefaultAsync(x => x.Id == generatorReadingId, ct);
        if (reading is null || reading.Generator.ActualPowerKW <= 0) return;
        var observed = new[] { reading.ActiveLoadKw, reading.PeakLoadKw }.Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty().Max();
        await AddConsistencyWarningsAsync(reading, ct);
        if (observed <= 0) { await ResolveOpenOverloadAsync(reading.GeneratorId, ct); return; }
        var overload = observed > reading.Generator.ActualPowerKW; // exactly 100% is not an overload
        var open = await db.OverloadEvents.SingleOrDefaultAsync(x => x.GeneratorId == reading.GeneratorId && x.ResolvedAtUtc == null, ct);
        if (!overload) { if (open is not null) { open.ResolvedAtUtc = DateTime.UtcNow; open.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); } return; }
        var percent = observed / reading.Generator.ActualPowerKW * 100m;
        if (open is not null) { open.LatestReadingId = reading.Id; open.PeakPercent = Math.Max(open.PeakPercent, percent); open.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); return; }
        var entity = new OverloadEvent { GeneratorId = reading.GeneratorId, TriggerReadingId = reading.Id, LatestReadingId = reading.Id, StartedAtUtc = reading.CapturedAtUtc, PeakPercent = percent, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.OverloadEvents.Add(entity); await db.SaveChangesAsync(ct);
        await PublishAsync(reading.Generator.SubStationId, "Operational overload", $"{reading.Generator.Name} exceeded its configured capacity ({percent:0.##}%).", "OverloadEvent", entity.Id, ct);
    }

    public async Task EvaluateOperatingLimitsAsync(CancellationToken ct = default)
    {
        var limits = await db.GeneratorOperatingLimits.Include(x => x.Generator).ToListAsync(ct);
        foreach (var limit in limits)
        {
            var reading = await db.OperationalGeneratorReadings.Where(r => r.GeneratorId == limit.GeneratorId && (r.Status == OperationalReadingStatus.Recorded || r.Status == OperationalReadingStatus.Approved))
                .OrderByDescending(r => r.CapturedAtUtc).FirstOrDefaultAsync(ct);
            if (reading is null) continue;
            var observed = ValueFor(limit.Parameter, reading); if (!observed.HasValue) continue;
            var breached = (limit.Minimum.HasValue && observed < limit.Minimum) || (limit.Maximum.HasValue && observed > limit.Maximum);
            var open = await db.OperatingLimitEpisodes.SingleOrDefaultAsync(x => x.GeneratorOperatingLimitId == limit.Id && x.ResolvedAtUtc == null, ct);
            if (!breached) { if (open is not null) { open.ResolvedAtUtc = DateTime.UtcNow; open.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); } continue; }
            if (open is not null) { open.ObservedValue = observed.Value; open.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); continue; }
            var episode = new OperatingLimitEpisode { GeneratorOperatingLimitId = limit.Id, TriggerReadingId = reading.Id, StartedAtUtc = reading.CapturedAtUtc, ObservedValue = observed.Value, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            db.OperatingLimitEpisodes.Add(episode); await db.SaveChangesAsync(ct);
            await PublishAsync(limit.Generator.SubStationId, "Operating limit exceeded", $"{limit.Generator.Name}: {limit.Parameter} is outside its configured limit.", "OperatingLimitEpisode", episode.Id, ct);
        }
    }

    private async Task AddConsistencyWarningsAsync(OperationalGeneratorReading reading, CancellationToken ct)
    {
        var warnings = ParseWarnings(reading.WarningsJson).ToList();
        if (reading.ActiveLoadKw.HasValue && reading.ApparentLoadKva.HasValue && reading.PowerFactor.HasValue && reading.ApparentLoadKva > 0)
        {
            var expected = reading.ApparentLoadKva.Value * reading.PowerFactor.Value;
            if (expected > 0 && Math.Abs(reading.ActiveLoadKw.Value - expected) / expected > .10m) warnings.Add("Active load differs from kVA × PF by more than 10%.");
        }
        var repeats = await db.OperationalGeneratorReadings.CountAsync(x => x.GeneratorId == reading.GeneratorId && x.ActiveLoadKw == reading.ActiveLoadKw && x.Id != reading.Id, ct);
        if (reading.ActiveLoadKw.HasValue && repeats >= 5) warnings.Add("Repeated active-load value requires review.");
        reading.WarningsJson = JsonSerializer.Serialize(warnings.Distinct());
        await db.SaveChangesAsync(ct);
    }

    private async Task ResolveOpenOverloadAsync(int generatorId, CancellationToken ct)
    {
        var open = await db.OverloadEvents.SingleOrDefaultAsync(x => x.GeneratorId == generatorId && x.ResolvedAtUtc == null, ct);
        if (open is null) return; open.ResolvedAtUtc = DateTime.UtcNow; open.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct);
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
    { try { await notifications.PublishScopedAsync(new CreateNotificationDto(subStationId, title, message, type, "Warning", null, type, id), null, ["operational_data"], ct); } catch { /* alerts cannot invalidate capture */ } }
    private static IReadOnlyList<string> ParseWarnings(string? json)
    { try { return string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<string>>(json) ?? []; } catch { return []; } }
}
