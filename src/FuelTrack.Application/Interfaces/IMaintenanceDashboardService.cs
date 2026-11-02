using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Interfaces;

public interface IMaintenanceDashboardService
{
    Task<MaintenanceDashboardDto> GetDashboardAsync(int? subStationId, int? stationId, CancellationToken ct = default);
}
