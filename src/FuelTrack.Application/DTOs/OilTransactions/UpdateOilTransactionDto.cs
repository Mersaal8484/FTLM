namespace FuelTrack.Application.DTOs.OilTransactions;

public record UpdateOilTransactionDto(
    int? OilLookupId,
    DateTime? TransactionDate,
    double? QuantityLiters,
    string? Reference,
    string? SupplierCompany,
    int? SourceSubStationId,
    int? DestinationSubStationId,
    string? Notes);