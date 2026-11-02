namespace FuelTrack.Application.DTOs.FuelMovements;

public record FuelMovementListDto(
    int Id,
    int SubStationId,
    string SubStationName,
    int FuelLookupId,
    string FuelLookupName,
    int? TransactionTypeLookupId,
    string? TransactionTypeLookupName,
    DateTime TransactionDate,
    double QuantityLiters,
    string? Reference,
    bool IsApproved,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    DateTime CreatedAt,
    string? SourceSubStationName,
    string? DestinationSubStationName,
    double? InvoiceQuantity,
    string? InvoiceNumber,
    int? CorrectedFromId,
    string? CorrectionReason);
