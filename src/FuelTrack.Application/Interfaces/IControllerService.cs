using FuelTrack.Application.DTOs.Controllers;

namespace FuelTrack.Application.Interfaces;

public interface IControllerService
{
    Task<ControllerDetailDto> CreateAsync(CreateControllerDto dto);
    Task<ControllerDetailDto> UpdateAsync(int id, CreateControllerDto dto);
    Task<ControllerDetailDto?> GetByIdAsync(int id);
    Task<List<ControllerListDto>> GetAllAsync();
    Task DeleteAsync(int id);
    Task<ControllerDetailDto> UpdateConnectionStatusAsync(int id, string status);
    Task<ControllerDetailDto?> AssignGeneratorAsync(int controllerId, int? generatorId);
}
