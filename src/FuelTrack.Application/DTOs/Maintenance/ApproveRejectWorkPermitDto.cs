namespace FuelTrack.Application.DTOs.Maintenance;

public record ApproveWorkPermitDto(
    string? Notes = null);

public record RejectWorkPermitDto(
    string Reason,
    string? Notes = null);
