namespace FuelTrack.Application.DTOs.Maintenance;

public record MaintenanceLogDto(
    int Id,
    string WorkPermitNumber,
    string? GeneratorName,
    DateTime LogDate,
    double? RunningHoursAtLog,
    string Action,
    string? Notes,
    string LoggedByName,
    DateTime CreatedAt);

public record MaintenanceDashboardDto(
    int TotalOpenPermits,
    int CriticalPermits,
    int OverdueInstalledParts,
    int LowStockPartsCount,
    int ActiveWorkPermits,
    List<WorkPermitListDto> RecentPermits,
    List<InstalledPartDto> UpcomingReplacements);
