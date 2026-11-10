using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Interfaces;

public interface IServiceDueService
{
    Task<Result<GeneratorMaintenanceTableDto>> GetGeneratorMaintenanceTableAsync(int generatorId, CancellationToken ct = default);
    Task<IReadOnlyList<GeneratorMaintenanceTableDto>> GetStationMaintenanceTablesAsync(int? subStationId = null, CancellationToken ct = default);
    Task<DueServiceDashboardSummaryDto> GetDueDashboardSummaryAsync(int? subStationId = null, CancellationToken ct = default);
}
