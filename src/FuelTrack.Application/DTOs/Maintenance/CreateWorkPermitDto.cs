namespace FuelTrack.Application.DTOs.Maintenance;

public record CreateWorkPermitDto(
    int SubStationId,
    int? GeneratorId,
    int MaintenanceTypeLookupId,
    int DepartmentLookupId,
    int PriorityLookupId,
    DateTime PermitDate,
    string ProblemDescription,
    string? ProblemCause,
    DateTime? PlannedStartDate,
    DateTime? PlannedEndDate,
    int? AssignedToId,
    string? Notes,
    List<CreateWorkTaskDto> Tasks);

public record UpdateWorkPermitDto(
    int Id,
    int? GeneratorId,
    int? MaintenanceTypeLookupId,
    int? DepartmentLookupId,
    int? PriorityLookupId,
    DateTime? PermitDate,
    string? ProblemDescription,
    string? ProblemCause,
    DateTime? PlannedStartDate,
    DateTime? PlannedEndDate,
    int? AssignedToId,
    string? CompletionNotes,
    string? RejectionReason,
    string? Notes,
    List<CreateWorkTaskDto>? Tasks);

public record UpdateWorkPermitStatusDto(
    int StatusLookupId,
    string? Notes,
    string? RejectionReason,
    DateTime? ActualStartDate,
    DateTime? ActualEndDate);
