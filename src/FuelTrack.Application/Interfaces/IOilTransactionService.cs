using FuelTrack.Application.DTOs.OilTransactions;

namespace FuelTrack.Application.Interfaces;

public interface IOilTransactionService
{
    Task<OilTransactionDetailDto> CreateAsync(CreateOilTransactionDto dto, int userId);
    Task<OilTransactionDetailDto> UpdateAsync(int id, UpdateOilTransactionDto dto, int userId);
    Task<OilTransactionDetailDto?> GetByIdAsync(int id);
    Task<List<OilTransactionListDto>> GetAllAsync();
    Task<List<OilTransactionListDto>> GetBySubStationAsync(int subStationId);
    Task<List<OilTransactionListDto>> GetByStationAsync(int stationId);
    Task<List<OilTransactionListDto>> GetByGeneratorAsync(int generatorId);
}
