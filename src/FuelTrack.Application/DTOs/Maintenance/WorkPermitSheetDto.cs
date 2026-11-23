namespace FuelTrack.Application.DTOs.Maintenance;

public record WorkPermitSheetDto(
    int Id,
    string PermitNumber,
    string StationName,
    int? GeneratorId,
    string? GeneratorName,
    string? GeneratorSerialNumber,
    string MaintenanceTypeName,
    string DepartmentName,
    string PriorityName,
    string StatusName,
    string StatusCode,
    DateTime PermitDate,
    string ProblemDescription,
    string? ProblemCause,
    DateTime? PlannedStartDate,
    DateTime? PlannedEndDate,
    DateTime? ActualStartDate,
    DateTime? ActualEndDate,
    double? RunningHoursAtCreation,
    string RequesterName,
    DateTime CreatedAt,
    string? ApproverName,
    DateTime? ApprovedAt,
    string? AssignedToName,
    string? CompletionNotes,
    string? RejectionReason,
    string? Notes,
    List<WorkPermitSheetTaskDto> Tasks,
    List<WorkPermitSheetDispatchedPartDto> DispatchedParts,
    List<WorkPermitSheetReplacementDto> Replacements,
    decimal TotalOilConsumedLitres,
    List<WorkPermitSheetLogDto> Logs);

public record WorkPermitSheetTaskDto(
    int Id,
    string TaskDescription,
    bool IsCompleted,
    DateTime? CompletedAt,
    string? CompletedByName);

public record WorkPermitSheetDispatchedPartDto(
    string PartName,
    string PartNumber,
    decimal QuantityRequested,
    decimal? QuantityApproved,
    string? UnitOfMeasure);

public record WorkPermitSheetReplacementDto(
    int Id,
    string ServiceItemName,
    decimal HoursAtService,
    string Kind,
    DateTime OccurredAtUtc,
    bool IsEarly,
    string? EarlyReplacementReason,
    decimal? IntervalHoursSnapshot,
    int? OilTransactionId,
    decimal OilConsumedLitres,
    string? OilTypeName,
    string? Notes);

public record WorkPermitSheetLogDto(
    int Id,
    string Action,
    string? Notes,
    string LoggedByName,
    DateTime LogDate);
