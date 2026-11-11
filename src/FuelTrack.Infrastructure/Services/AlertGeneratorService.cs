using FuelTrack.Application.DTOs.Notifications;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

/// <summary>Produces deduplicated, scope-aware in-app stock and maintenance alerts.</summary>
public sealed class AlertGeneratorService : IAlertGeneratorService
{
    private static readonly TimeSpan RepeatWindow = TimeSpan.FromHours(24);
    private readonly FuelTrackDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ICustomerFeatureService _customerFeatureService;

    public AlertGeneratorService(
        FuelTrackDbContext db,
        INotificationService notifications,
        ICustomerFeatureService? customerFeatureService = null)
    {
        _db = db;
        _notifications = notifications;
        _customerFeatureService = customerFeatureService ?? new CustomerFeatureService(db);
    }

    public async Task<int> CheckFuelStockLevelsAsync(CancellationToken ct = default)
    {
        var tanksWithStock = await _db.FuelTanks
            .AsNoTracking()
            .Include(t => t.SubStation)
            .Where(t => t.ActualStockLiters.HasValue && t.MinAlertLiters.HasValue)
            .ToListAsync(ct);
        var tanks = tanksWithStock.Where(t =>
            TankVolumeService.UsableLitres((decimal)t.ActualStockLiters!.Value, TankVolumeService.DeadStockLitres(t))
            <= (decimal)t.MinAlertLiters!.Value).ToList();

        var created = 0;
        foreach (var tank in tanks)
        {
            if (await WasRecentlyRaisedAsync("fuel_low", "FuelTank", tank.Id, ct)) continue;
            if (await CreateAndPublishAsync(new CreateNotificationDto(
                tank.SubStationId,
                "Low fuel stock",
                $"{tank.Name} at {tank.SubStation.Name} has {TankVolumeService.UsableLitres((decimal)tank.ActualStockLiters!.Value, TankVolumeService.DeadStockLitres(tank)):0.##} L usable fuel (alert level {tank.MinAlertLiters:0.##} L).",
                "fuel_low", "danger", null, "FuelTank", tank.Id), new[] { "fuel_tanks", "fuel_movements" }, ct))
                created++;
        }
        return created;
    }

    public async Task<int> CheckOilStockLevelsAsync(CancellationToken ct = default)
    {
        var oils = await _db.Oils
            .AsNoTracking()
            .Include(o => o.OilLookup)
            .Include(o => o.SubStation)
            .Include(o => o.Station)
            .Where(o => o.CurrentStock <= o.MinStock && (o.SubStationId.HasValue || o.StationId.HasValue))
            .ToListAsync(ct);

        var created = 0;
        foreach (var oil in oils)
        {
            if (await WasRecentlyRaisedAsync("oil_low", "Oil", oil.Id, ct)) continue;
            if (await CreateAndPublishAsync(new CreateNotificationDto(
                oil.SubStationId,
                "Low oil stock",
                $"{oil.OilLookup.Name} at {oil.SubStation?.Name ?? oil.Station?.Name ?? "the selected location"} is at {oil.CurrentStock:0.##} (minimum {oil.MinStock:0.##}).",
                "oil_low", "danger", null, "Oil", oil.Id), new[] { "oil_tanks", "oil_transactions" }, ct))
                created++;
        }
        return created;
    }

    public async Task<int> CheckInstalledPartsReplacementAsync(CancellationToken ct = default)
    {
        // Gated: when operational data feature is active in new mode, Phase 4 service-due engine
        // is the single source of truth for maintenance/service due alerts (Brief V4 §8).
        if (await _customerFeatureService.IsOperationalDataNewModeAsync(ct))
        {
            return 0;
        }

        var parts = await _db.InstalledParts
            .AsNoTracking()
            .Include(p => p.SparePart)
            .Include(p => p.Generator).ThenInclude(g => g.SubStation)
            .Include(p => p.Generator).ThenInclude(g => g.Controller)
            .Where(p => p.IsActive && p.EstimatedReplacementAtHours.HasValue && p.Generator.Controller != null)
            .ToListAsync(ct);

        var created = 0;
        foreach (var part in parts)
        {
            var operatingHours = part.Generator.Controller!.OperatingHours;
            var dueAt = part.EstimatedReplacementAtHours!.Value;
            var remaining = dueAt - operatingHours;
            var type = operatingHours >= dueAt ? "part_replacement_overdue" : "part_replacement_due";
            var isDueSoon = remaining <= Math.Max(50d, dueAt * 0.10d);
            if (!isDueSoon || await WasRecentlyRaisedAsync(type, "InstalledPart", part.Id, ct)) continue;

            var severity = type == "part_replacement_overdue" ? "danger" : "warning";
            var state = type == "part_replacement_overdue"
                ? $"is overdue by {Math.Abs(remaining):0.##} operating hours"
                : $"is due in {remaining:0.##} operating hours";

            if (await CreateAndPublishAsync(new CreateNotificationDto(
                part.Generator.SubStationId,
                "Spare part replacement required",
                $"{part.SparePart.Name} on generator {part.Generator.Name} {state}.",
                type, severity, null, "InstalledPart", part.Id), new[] { "maintenance" }, ct))
                created++;
        }
        return created;
    }

    private Task<bool> WasRecentlyRaisedAsync(string type, string entityType, int entityId, CancellationToken ct) =>
        _db.Notifications.AnyAsync(n => n.NotificationType == type && n.RelatedEntityType == entityType &&
            n.RelatedEntityId == entityId && n.CreatedAt >= DateTime.UtcNow.Subtract(RepeatWindow), ct);

    private async Task<bool> CreateAndPublishAsync(CreateNotificationDto dto, IReadOnlyCollection<string> moduleKeys, CancellationToken ct)
    {
        return await _notifications.PublishScopedAsync(dto, null, moduleKeys, ct) is not null;
    }
}
