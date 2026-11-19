namespace FuelTrack.Application.DTOs.Maintenance;

public record CreateWorkPermitDto(
    int SubStationId = 0,
    int? GeneratorId = null,
    int MaintenanceTypeLookupId = 0,
    int DepartmentLookupId = 0,
    int PriorityLookupId = 0,
    DateTime PermitDate = default,
    string ProblemDescription = "",
    string? ProblemCause = null,
    DateTime? PlannedStartDate = null,
    DateTime? PlannedEndDate = null,
    int? AssignedToId = null,
    string? Notes = null,
    List<CreateWorkTaskDto>? Tasks = null);

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
