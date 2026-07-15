namespace FuelTrack.Application.DTOs.Audits;

public record AuditFilterParams(
    int Page = 1,
    int PageSize = 20,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int? UserId = null,
    string? Action = null,
    string? EntityName = null);
