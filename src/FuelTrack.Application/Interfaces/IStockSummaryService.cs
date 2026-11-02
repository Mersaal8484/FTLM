using FuelTrack.Application.DTOs.FuelMovements;

namespace FuelTrack.Application.Interfaces;

public interface IStockSummaryService
{
    Task<List<FuelStockSummaryDto>> GetFuelStockSummaryAsync(DateTime fromDate, DateTime toDate, int? userId);
    Task<List<OilStockSummaryDto>> GetOilStockSummaryAsync(DateTime fromDate, DateTime toDate, int? userId);
}
