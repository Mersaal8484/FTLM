using FuelTrack.Application.DTOs.OilTransactions;
using FuelTrack.Application.DTOs.Oils;

namespace FuelTrack.Application.Interfaces;

public interface IOilService
{
    Task<OilDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<OilListDto>> GetAllAsync(CancellationToken ct = default);
    Task<List<OilListDto>> GetAllByStationAsync(int stationId, CancellationToken ct = default);
    Task<List<OilListDto>> GetAllBySubStationAsync(int subStationId, CancellationToken ct = default);
    Task<OilListDto> CreateAsync(CreateOilDto dto, int userId, CancellationToken ct = default);
    Task<OilDetailDto> UpdateAsync(int id, UpdateOilDto dto, int userId, CancellationToken ct = default);
    Task DeleteAsync(int id, int userId, CancellationToken ct = default);
    Task<OilTransactionDetailDto> AddToGeneratorAsync(AddToGeneratorDto dto, int userId, CancellationToken ct = default);
}
