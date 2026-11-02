using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Interfaces;

public interface IInstalledPartService
{
    Task<List<InstalledPartDto>> GetAllAsync(int? generatorId = null, CancellationToken ct = default);
    Task<Result<InstalledPartDto>> InstallAsync(CreateInstalledPartDto dto, int userId, CancellationToken ct = default);
    Task<Result<InstalledPartDto>> RemoveAsync(int id, RemoveInstalledPartDto dto, int userId, CancellationToken ct = default);
    Task<List<InstalledPartDto>> GetByGeneratorAsync(int generatorId, bool activeOnly, CancellationToken ct = default);
    Task<List<InstalledPartDto>> GetOverdueAsync(int? subStationId, CancellationToken ct = default);
    Task<List<InstalledPartDto>> GetUpcomingReplacementsAsync(int? subStationId, double hoursThreshold, CancellationToken ct = default);
    Task<bool> CheckEarlyReplacementAsync(int sparePartId, int generatorId, CancellationToken ct = default);
}
