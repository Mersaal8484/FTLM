namespace FuelTrack.Application.DTOs.Maintenance;

public record InstalledPartDto(
    int Id,
    int GeneratorId,
    string GeneratorName,
    int SparePartId,
    string SparePartName,
    string PartNumber,
    string? WorkPermitNumber,
    DateTime InstallationDate,
    double InstallationHours,
    int? ReplacementIntervalHours,
    double? EstimatedReplacementAtHours,
    double? CurrentGeneratorHours,
    double? HoursUntilReplacement,
    bool IsOverdue,
    bool IsActive,
    DateTime? RemovedAt,
    double? RemovedAtHours,
    string? EarlyReplacementReason,
    string? Notes);

public record CreateInstalledPartDto(
    int GeneratorId,
    int SparePartId,
    int? WorkPermitId,
    int? DispatchRequestItemId,
    DateTime InstallationDate,
    double InstallationHours,
    int? ReplacementIntervalHours,
    string? Notes);

public record RemoveInstalledPartDto(
    double RemovedAtHours,
    string? EarlyReplacementReason,
    string? Notes);
