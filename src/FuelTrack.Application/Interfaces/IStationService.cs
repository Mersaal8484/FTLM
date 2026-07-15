using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Stations;

namespace FuelTrack.Application.Interfaces;

public interface IStationService
{
    Task<Result<StationDetailDto>> CreateAsync(CreateStationDto dto, int userId);
    Task<Result<StationDetailDto>> UpdateAsync(int id, UpdateStationDto dto, int userId);
    Task<Result> DeleteAsync(int id);
    Task<StationDetailDto?> GetByIdAsync(int id);
    Task<List<StationListDto>> GetAllAsync();
    Task<List<StationListDto>> GetByBranchAsync(int branchId);
}
