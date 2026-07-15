using FuelTrack.Application.DTOs.Meters;

namespace FuelTrack.Application.Interfaces;

public interface IMeterService
{
    Task<MeterDetailDto> CreateAsync(CreateMeterDto dto);
    Task<MeterDetailDto> UpdateAsync(int id, CreateMeterDto dto);
    Task<MeterDetailDto?> GetByIdAsync(int id);
    Task<List<MeterListDto>> GetAllAsync();
    Task DeleteAsync(int id);
}
