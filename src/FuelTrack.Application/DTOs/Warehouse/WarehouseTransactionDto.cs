namespace FuelTrack.Application.DTOs.Warehouse;

public record WarehouseTransactionDto(
    int Id,
    string SparePartName,
    string PartNumber,
    string TransactionTypeName,
    DateTime TransactionDate,
    decimal Quantity,
    decimal BalanceBefore,
    decimal BalanceAfter,
    int? ReferenceId,
    string? ReferenceType,
    string? Notes,
    string CreatedByName,
    DateTime CreatedAt);

public record WarehouseReportDto(
    int StationId,
    string StationName,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalSupplied,
    decimal TotalDispatched,
    decimal NetChange,
    List<SparePartListDto> LowStockItems,
    List<WarehouseTransactionDto> Items);
