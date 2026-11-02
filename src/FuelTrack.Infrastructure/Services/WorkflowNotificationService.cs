using FuelTrack.Application.DTOs.Notifications;
using FuelTrack.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class WorkflowNotificationService : IWorkflowNotificationService
{
    private readonly FuelTrackDbContext _db;
    private readonly INotificationService _notifications;

    public WorkflowNotificationService(FuelTrackDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task NotifyReadingAsync(int generatorId, int readingId, string generatorName, string notificationType, int? sentById, string? reason = null, CancellationToken ct = default)
    {
        var subStationId = await _db.Generators.Where(g => g.Id == generatorId).Select(g => (int?)g.SubStationId).FirstOrDefaultAsync(ct);
        if (!subStationId.HasValue) return;
        var action = notificationType switch { "reading_approved" => "approved", "reading_rejected" => "rejected", _ => "submitted for approval" };
        var message = reason is null ? $"Reading for generator {generatorName} was {action}." : $"Reading for generator {generatorName} was rejected: {reason}";
        await _notifications.PublishScopedAsync(new CreateNotificationDto(subStationId, "Generator reading update", message, notificationType,
            notificationType == "reading_rejected" ? "danger" : "info", null, "Reading", readingId), sentById, new[] { "daily_readings" }, ct);
    }

    public async Task NotifyInventoryAsync(int inventoryId, int subStationId, string notificationType, int? sentById, CancellationToken ct = default)
    {
        await _notifications.PublishScopedAsync(new CreateNotificationDto(subStationId, "Periodic inventory update",
            notificationType == "inventory_approved" ? "A periodic inventory was approved." : "A periodic inventory was submitted for approval.",
            notificationType, "info", null, "PeriodicInventory", inventoryId), sentById, new[] { "periodic_inventory" }, ct);
    }

    public async Task NotifyWorkPermitAssignedAsync(int workPermitId, int subStationId, string permitNumber, int? assignedToId, int? sentById, CancellationToken ct = default)
    {
        var recipients = assignedToId.HasValue ? new List<int> { assignedToId.Value } : null;
        await _notifications.PublishScopedAsync(new CreateNotificationDto(subStationId, "Work permit assignment",
            $"Work permit {permitNumber} has been assigned to you.", "work_permit_assigned", "info", recipients, "WorkPermit", workPermitId), sentById, new[] { "maintenance" }, ct);
    }

    public async Task NotifySupplyRequestedAsync(int supplyId, int stationId, string supplyNumber, int? sentById, CancellationToken ct = default)
    {
        var subStationIds = await _db.SubStations.Where(s => s.StationId == stationId && s.IsActive)
            .Select(s => s.Id).ToListAsync(ct);
        await Task.WhenAll(subStationIds.Select(subStationId => _notifications.PublishScopedAsync(
            new CreateNotificationDto(subStationId, "Supply request submitted", $"Supply request {supplyNumber} was submitted for processing.",
                "supply_requested", "info", null, "WarehouseSupply", supplyId), sentById, new[] { "warehouse" }, ct)));
    }
}
