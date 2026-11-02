namespace FuelTrack.Application.Interfaces;

/// <summary>Creates scope- and permission-aware notifications for operational workflow events.</summary>
public interface IWorkflowNotificationService
{
    Task NotifyReadingAsync(int generatorId, int readingId, string generatorName, string notificationType, int? sentById, string? reason = null, CancellationToken ct = default);
    Task NotifyInventoryAsync(int inventoryId, int subStationId, string notificationType, int? sentById, CancellationToken ct = default);
    Task NotifyWorkPermitAssignedAsync(int workPermitId, int subStationId, string permitNumber, int? assignedToId, int? sentById, CancellationToken ct = default);
    Task NotifySupplyRequestedAsync(int supplyId, int stationId, string supplyNumber, int? sentById, CancellationToken ct = default);
}
