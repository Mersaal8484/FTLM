namespace FuelTrack.Application.DTOs.Correction;

public record CorrectionDto(
    int Id,
    int OriginalEntityId,
    string EntityType,
    string? Reason,
    string? OldValues,
    string? NewValues,
    string? CorrectedByUserName,
    DateTime CreatedAt);

public record CreateCorrectionDto(
    int EntityId,
    string EntityType,
    string Reason,
    string? OldValues,
    string? NewValues);
