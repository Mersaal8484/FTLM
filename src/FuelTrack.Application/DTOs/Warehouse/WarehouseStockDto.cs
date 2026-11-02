namespace FuelTrack.Application.DTOs.Warehouse;

public record WarehouseStockDto(
    int SparePartId,
    string PartNumber,
    string SparePartName,
    string Category,
    string UnitName,
    int StationId,
    string StationName,
    decimal CurrentQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    int MinStockQuantity,
    bool IsLowStock);
