namespace FuelTrack.Application.DTOs.OilTransactions;

public record CreateOilTransactionDto(
    int SubStationId,
    int OilLookupId,
    int? OilId,
    int TransactionTypeLookupId,
    DateTime TransactionDate,
    double QuantityLiters,
    // بيانات الوارد
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
    // النقل
    int? SourceSubStationId,
    int? DestinationSubStationId,
    // ملاحظات
    string? Notes);
