using FuelTrack.Application.DTOs.Warehouse;

namespace FuelTrack.Application.Interfaces;

public interface IWarehouseStockService
{
    Task<List<WarehouseStockDto>> GetByStationAsync(int stationId, CancellationToken ct = default);
    Task<List<WarehouseStockDto>> GetAllAsync(int? stationId = null, CancellationToken ct = default);
    Task<List<WarehouseStockDto>> GetByPartAsync(int sparePartId, CancellationToken ct = default);
    Task<List<WarehouseStockDto>> GetLowStockAsync(int? stationId, CancellationToken ct = default);
    Task AdjustStockAsync(int sparePartId, int stationId, decimal delta, string reason, int userId, CancellationToken ct = default);
}
