namespace FuelTrack.Application.DTOs.FuelMovements;

public record CreateFuelMovementDto(
    int SubStationId,
    int FuelLookupId,
    int? TransactionTypeLookupId,
    DateTime TransactionDate,
    double QuantityLiters,
    string? Reference,
    // بيانات الوارد المعقدة
    string? SupplierCompany,
    string? DriverName,
    string? DriverIdCard,
    string? TruckNumber,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    double? InvoiceQuantity,
    // قراءات عداد
    int? MeterId,
    double? PrevMeterVisualReading,
    double? CurrentMeterVisualReading,
    double? PrevMeterStandardReading,
    double? CurrentMeterStandardReading,
    // النقل
    int? SourceSubStationId,
    int? DestinationSubStationId,
    // التصحيح
    int? CorrectedFromId,
    string? CorrectionReason,
    // ملاحظات
    string? Notes);
