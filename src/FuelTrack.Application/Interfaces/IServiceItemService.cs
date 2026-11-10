using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Interfaces;

public interface IServiceItemService
{
    Task<IReadOnlyList<ServiceItemDto>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Result<ServiceItemDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<ServiceItemDto>> CreateAsync(CreateServiceItemDto dto, int userId, CancellationToken ct = default);
    Task<Result<ServiceItemDto>> UpdateAsync(int id, UpdateServiceItemDto dto, int userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(int id, int userId, CancellationToken ct = default);
}
