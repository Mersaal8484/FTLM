namespace FuelTrack.Application.DTOs.Rollback;

public record RollbackListDto(
    int Id,
    int UserId,
    string UserName,
    string Action,
    string EntityName,
    string EntityId,
    DateTime Timestamp);
