using FuelTrack.Application.DTOs.Efficiency;
using FuelTrack.Application.DTOs.Inventory;

namespace FuelTrack.Application.Interfaces;

public interface IPeriodicInventoryService
{
    Task<PeriodicInventoryDto> CreateAsync(CreatePeriodicInventoryDto dto, int userId);
    Task<PeriodicInventoryDto?> GetByIdAsync(int id);
    Task<List<PeriodicInventoryListDto>> GetBySubStationAsync(int subStationId);
    Task<List<PeriodicInventoryListDto>> GetAllAsync(string? status = null);
    Task<PeriodicInventoryDto> UpdateAsync(int id, CreatePeriodicInventoryDto dto, int userId);
    Task ApproveAsync(int id, int approvedById);
    Task DeleteAsync(int id);
    Task<EfficiencyResultDto> CalculateEfficiencyAsync(int id);
}
