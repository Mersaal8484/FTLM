using FuelTrack.Application.DTOs.FuelTanks;

namespace FuelTrack.Application.Interfaces;

public interface IFuelTankService
{
    Task<FuelTankDetailDto> CreateAsync(CreateFuelTankDto dto, int userId);
    Task<FuelTankDetailDto> UpdateAsync(int id, UpdateFuelTankDto dto, int userId);
    Task DeleteAsync(int id);
    Task<FuelTankDetailDto?> GetByIdAsync(int id);
    Task<List<FuelTankListDto>> GetAllAsync();
    Task<List<FuelTankListDto>> GetBySubStationAsync(int subStationId);
}
