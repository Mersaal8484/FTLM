using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Warehouse;

namespace FuelTrack.Application.Interfaces;

public interface IWarehouseSupplyService
{
    Task<Result<WarehouseSupplyDetailDto>> CreateAsync(CreateWarehouseSupplyDto dto, int userId, CancellationToken ct = default);
    Task<Result<WarehouseSupplyDetailDto>> UpdateAsync(int id, UpdateWarehouseSupplyDto dto, int userId, CancellationToken ct = default);
    Task<Result<WarehouseSupplyDetailDto>> ConfirmAsync(int id, int userId, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<WarehouseSupplyDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<WarehouseSupplyListDto>> GetByStationAsync(int stationId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<WarehouseSupplyListDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
}
