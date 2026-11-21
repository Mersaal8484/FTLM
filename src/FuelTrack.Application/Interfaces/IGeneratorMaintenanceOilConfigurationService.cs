using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Interfaces;

public interface IGeneratorMaintenanceOilConfigurationService
{
    Task<IReadOnlyList<GeneratorMaintenanceOilConfigurationDto>> GetAllAsync(int? generatorId = null, CancellationToken ct = default);
    Task<Result<GeneratorMaintenanceOilConfigurationDto>> SaveAsync(SaveGeneratorMaintenanceOilConfigurationDto dto, int userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(int id, int userId, CancellationToken ct = default);
}
