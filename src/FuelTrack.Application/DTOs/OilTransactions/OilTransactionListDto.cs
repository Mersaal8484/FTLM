namespace FuelTrack.Application.DTOs.OilTransactions;

public record OilTransactionListDto(
    int Id,
    int SubStationId,
    string SubStationName,
    int OilLookupId,
    string OilLookupName,
    int? OilId,
    string? OilName,
    int TransactionTypeLookupId,
    string TransactionTypeLookupName,
    DateTime TransactionDate,
    double QuantityLiters,
    string? Reference,
    bool IsApproved,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    DateTime CreatedAt);
