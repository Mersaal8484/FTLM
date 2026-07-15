namespace FuelTrack.Application.DTOs.Inventory;

public record PeriodicInventoryListDto(
    int Id,
    int SubStationId,
    string SubStationName,
    int Year,
    int Month,
    string PeriodType,
    DateTime InventoryDate,
    string? Notes,
    DateTime CreatedAt);
