namespace FuelTrack.Application.DTOs.Warehouse;

public record WarehouseSupplyListDto(
    int Id,
    string SupplyNumber,
    string StationName,
    DateTime SupplyDate,
    string? SupplierName,
    string? InvoiceNumber,
    decimal? TotalAmount,
    string StatusName,
    int ItemCount,
    DateTime CreatedAt);

public record WarehouseSupplyDetailDto(
    int Id,
    string SupplyNumber,
    int StationId,
    string StationName,
    DateTime SupplyDate,
    string? SupplierName,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    decimal? TotalAmount,
    int StatusLookupId,
    string StatusName,
    string? Notes,
    DateTime CreatedAt,
    List<SupplyItemDto> Items);
