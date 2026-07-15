using FuelTrack.Application.DTOs.Screens;
using FuelTrack.Application.Interfaces;

namespace FuelTrack.Infrastructure.Services;

public class ScreenService : IScreenService
{
    public Task<ScreenDetailDto> CreateAsync(CreateScreenDto dto)
    {
        throw new NotSupportedException("Screen entity has been removed.");
    }

    public Task<ScreenDetailDto> UpdateAsync(int id, UpdateScreenDto dto)
    {
        throw new NotSupportedException("Screen entity has been removed.");
    }

    public Task<ScreenDetailDto?> GetByIdAsync(int id)
    {
        throw new NotSupportedException("Screen entity has been removed.");
    }

    public Task<List<ScreenListDto>> GetAllAsync()
    {
        throw new NotSupportedException("Screen entity has been removed.");
    }

    public Task DeleteAsync(int id)
    {
        throw new NotSupportedException("Screen entity has been removed.");
    }
}
