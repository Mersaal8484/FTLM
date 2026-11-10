using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Interfaces;

public interface IServiceIntervalDefinitionService
{
    Task<IReadOnlyList<ServiceIntervalDefinitionDto>> GetAllAsync(string? model = null, CancellationToken ct = default);
    Task<Result<ServiceIntervalDefinitionDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<ServiceIntervalDefinitionDto>> SaveAsync(SaveServiceIntervalDefinitionDto dto, int userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(int id, int userId, CancellationToken ct = default);
}
