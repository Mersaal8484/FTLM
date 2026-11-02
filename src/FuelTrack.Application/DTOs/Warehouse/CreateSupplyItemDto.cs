namespace FuelTrack.Application.DTOs.Warehouse;

public record CreateSupplyItemDto(
    int SparePartId,
    decimal Quantity,
    decimal UnitPrice,
    string? SerialNumber,
    string? Notes);

public record SupplyItemDto(
    int Id,
    int SparePartId,
    string PartNumber,
    string SparePartName,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string? SerialNumber,
    string? Notes);
