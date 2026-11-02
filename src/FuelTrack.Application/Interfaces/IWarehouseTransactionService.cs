using FuelTrack.Application.DTOs.Warehouse;

namespace FuelTrack.Application.Interfaces;

public interface IWarehouseTransactionService
{
    Task<PagedResult<WarehouseTransactionDto>> GetByPartAsync(int sparePartId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<WarehouseTransactionDto>> GetByStationAsync(int stationId, DateTime startDate, DateTime endDate, int page, int pageSize, CancellationToken ct = default);
}
