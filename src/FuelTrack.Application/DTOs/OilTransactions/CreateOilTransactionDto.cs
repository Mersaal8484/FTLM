namespace FuelTrack.Application.DTOs.OilTransactions;

public record CreateOilTransactionDto(
    int SubStationId,
    int OilLookupId,
    int TransactionTypeLookupId,
    DateTime TransactionDate,
    double QuantityLiters,
    string? Reference,
    string? SupplierCompany,
    int? SourceSubStationId,
    int? DestinationSubStationId,
    string? Notes);