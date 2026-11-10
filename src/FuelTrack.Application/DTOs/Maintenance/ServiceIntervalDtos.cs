using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.DTOs.Maintenance;

public sealed record ServiceItemDto(
    int Id,
    string Code,
    string NameAr,
    string NameEn,
    int SortOrder,
    decimal? SuggestedIntervalHours,
    bool IsActive);

public sealed record CreateServiceItemDto(
    string Code,
    string NameAr,
    string NameEn,
    int SortOrder = 0,
    decimal? SuggestedIntervalHours = null,
    bool IsActive = true);

public sealed record UpdateServiceItemDto(
    string NameAr,
    string NameEn,
    int SortOrder,
    decimal? SuggestedIntervalHours,
    bool IsActive);

public sealed record ServiceIntervalDefinitionDto(
    int Id,
    string GeneratorModelKey,
    int ServiceItemId,
    string ServiceItemCode,
    string ServiceItemNameAr,
    string ServiceItemNameEn,
    decimal IntervalHours,
    ServiceIntervalImportance Importance,
    decimal? WarningLeadHours,
    decimal EffectiveLeadHours,
    string? Notes);

public sealed record SaveServiceIntervalDefinitionDto(
    string GeneratorModelKey,
    int ServiceItemId,
    decimal IntervalHours,
    ServiceIntervalImportance Importance = ServiceIntervalImportance.Normal,
    decimal? WarningLeadHours = null,
    string? Notes = null);

public sealed record RecordReplacementRequestDto(
    int GeneratorId,
    int ServiceItemId,
    decimal HoursAtService,
    DateTime OccurredAtUtc,
    int? WorkPermitId = null,
    string? EarlyReplacementReason = null,
    string? Notes = null,
    Guid ClientRequestId = default);

public sealed record BulkBaselineRowDto(
    int ServiceItemId,
    decimal HoursAtService,
    DateTime OccurredAtUtc,
    string? Notes = null);

public sealed record BulkBaselineRequestDto(
    int GeneratorId,
    List<BulkBaselineRowDto> Items,
    Guid ClientRequestId = default);

public sealed record MultiItemReplacementRowDto(
    int ServiceItemId,
    string? EarlyReplacementReason = null,
    string? Notes = null);

public sealed record MultiItemReplacementRequestDto(
    int GeneratorId,
    decimal HoursAtService,
    DateTime OccurredAtUtc,
    int? WorkPermitId,
    List<MultiItemReplacementRowDto> Items,
    Guid ClientRequestId = default);

public sealed record BulkBaselineRowResultDto(
    int ServiceItemId,
    string? ServiceItemCode,
    bool Success,
    int? RecordId,
    string? ErrorMessage);

public sealed record BulkBaselineResultDto(
    int GeneratorId,
    int TotalRequested,
    int SucceededCount,
    int FailedCount,
    List<BulkBaselineRowResultDto> RowResults);

public sealed record ServiceReplacementRecordDto(
    int Id,
    int GeneratorId,
    string GeneratorName,
    int ServiceItemId,
    string ServiceItemCode,
    string ServiceItemNameAr,
    string ServiceItemNameEn,
    ServiceRecordKind Kind,
    decimal HoursAtService,
    DateTime OccurredAtUtc,
    int? WorkPermitId,
    bool IsEarly,
    string? EarlyReplacementReason,
    decimal? IntervalHoursSnapshot,
    string? Notes,
    DateTime? VoidedAtUtc,
    string? VoidReason,
    int CreatedBy,
    DateTime CreatedAt);

public sealed record VoidReplacementRecordDto(
    string Reason);

public sealed record GeneratorMaintenanceItemDueDto(
    int ServiceItemId,
    string ServiceItemCode,
    string ServiceItemNameAr,
    string ServiceItemNameEn,
    decimal IntervalHours,
    ServiceIntervalImportance Importance,
    decimal? LastServiceHours,
    DateTime? LastServiceOccurredAtUtc,
    ServiceRecordKind? LastServiceKind,
    int? LastServiceRecordId,
    decimal? DueAtHours,
    decimal? RemainingHours,
    decimal OverdueByHours,
    ServiceDueStatus Status,
    decimal ProgressFraction,
    decimal WarningLeadHours,
    bool IsMeterResetAfterLastService,
    string LabelAr,
    string LabelEn,
    string? DefinitionLink = null);

public sealed record GeneratorMaintenanceTableDto(
    int GeneratorId,
    string GeneratorName,
    string? Model,
    string NormalizedModelKey,
    int SubStationId,
    string SubStationName,
    decimal? CurrentOfficialHours,
    DateTime? LastClosingAtUtc,
    List<GeneratorMaintenanceItemDueDto> Items,
    int DueOrOverdueCount);

public sealed record DueServiceDashboardSummaryDto(
    int TotalDueCount,
    int TotalOverdueCount,
    List<GeneratorMaintenanceTableDto> GeneratorsWithDue);
