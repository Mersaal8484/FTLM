namespace FuelTrack.Application.DTOs.Audits;

public record AuditListDto(
    int Id,
    int UserId,
    string UserName,
    string Action,
    string EntityName,
    string EntityId,
    string? Method,
    string? Url,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    DateTime Timestamp);
