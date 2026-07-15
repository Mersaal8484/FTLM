namespace FuelTrack.Application.DTOs.OilTransactions;

public record UpdateOilTransactionDto(
    int? OilLookupId,
    int? OilId,
    int? TransactionTypeLookupId,
    DateTime? TransactionDate,
    double? QuantityLiters,
    string? SupplierCompany,
    string? DriverName,
    string? DriverIdCard,
    string? TruckNumber,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    double? InvoiceQuantity,
    string? WarehouseReceiptNumber,
    DateTime? WarehouseReceiptDate,
    double? WarehouseQuantity,
    int? SourceSubStationId,
    int? DestinationSubStationId,
    string? Notes);
