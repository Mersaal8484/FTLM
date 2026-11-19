namespace FuelTrack.Application.DTOs.OilTransactions;

public record CreateOilTransactionDto(
    int SubStationId = 0,
    int OilLookupId = 0,
    int TransactionTypeLookupId = 0,
    DateTime TransactionDate = default,
    double QuantityLiters = 0,
    string? Reference = null,
    string? SupplierCompany = null,
    int? SourceSubStationId = null,
    int? DestinationSubStationId = null,
    string? Notes = null);
