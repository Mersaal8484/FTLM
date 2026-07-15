namespace FuelTrack.Application.DTOs.Rollback;

public record RollbackCheckResultDto(
    bool CanRollback,
    string? Reason,
    string? EntityName,
    string? EntityId,
    string? Summary);
