namespace FuelTrack.Application.DTOs.Warehouse;

public record CreateWarehouseSupplyDto(
    int StationId,
    DateTime SupplyDate,
    string? SupplierName,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    decimal? TotalAmount,
    string? Notes,
    List<CreateSupplyItemDto> Items);

public record UpdateWarehouseSupplyDto(
    int Id,
    int StationId,
    DateTime SupplyDate,
    string? SupplierName,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    decimal? TotalAmount,
    string? Notes,
    List<CreateSupplyItemDto>? Items);
