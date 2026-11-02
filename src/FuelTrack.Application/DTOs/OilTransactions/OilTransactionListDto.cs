namespace FuelTrack.Application.DTOs.OilTransactions;

public record OilTransactionListDto(
    int Id,
    int SubStationId,
    string SubStationName,
    int OilLookupId,
    string OilLookupName,
    int TransactionTypeLookupId,
    string TransactionTypeLookupName,
    DateTime TransactionDate,
    double QuantityLiters,
    string? Reference,
    int? SourceSubStationId,
    string? SourceSubStationName,
    int? DestinationSubStationId,
    string? DestinationSubStationName,
    bool IsApproved,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    DateTime CreatedAt,
    int? CorrectedFromId,
    string? CorrectionReason);