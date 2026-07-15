using FuelTrack.Application.DTOs.Screens;

namespace FuelTrack.Application.Interfaces;

public interface IScreenService
{
    Task<ScreenDetailDto> CreateAsync(CreateScreenDto dto);
    Task<ScreenDetailDto> UpdateAsync(int id, UpdateScreenDto dto);
    Task<ScreenDetailDto?> GetByIdAsync(int id);
    Task<List<ScreenListDto>> GetAllAsync();
    Task DeleteAsync(int id);
}
