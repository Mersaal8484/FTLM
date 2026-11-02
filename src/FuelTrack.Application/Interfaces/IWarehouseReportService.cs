using FuelTrack.Application.DTOs.Warehouse;

namespace FuelTrack.Application.Interfaces;

public interface IWarehouseReportService
{
    Task<List<WarehouseStockDto>> GetStockReportAsync(int? stationId, CancellationToken ct = default);
    Task<WarehouseReportDto> GetMovementReportAsync(int stationId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<List<WarehouseStockDto>> GetLowStockAlertAsync(CancellationToken ct = default);
    Task<List<WarehouseTransactionDto>> GetDispatchByGeneratorAsync(int generatorId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<List<WarehouseTransactionDto>> GetDispatchBySubStationAsync(int subStationId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<List<WarehouseSupplyListDto>> GetSupplyReportAsync(int stationId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<WarehouseReportDto> GetPeriodicInventoryReportAsync(int stationId, string type, int year, int period, CancellationToken ct = default);
}
