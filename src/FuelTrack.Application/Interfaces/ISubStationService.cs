using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.SubStations;

namespace FuelTrack.Application.Interfaces;

public interface ISubStationService
{
    Task<Result<SubStationDetailDto>> CreateAsync(CreateSubStationDto dto, int userId);
    Task<Result<SubStationDetailDto>> UpdateAsync(int id, UpdateSubStationDto dto, int userId);
    Task<Result> DeleteAsync(int id);
    Task<SubStationDetailDto?> GetByIdAsync(int id);
    Task<List<SubStationListDto>> GetAllAsync(bool includeInactive = false);
    Task<List<SubStationListDto>> GetByStationAsync(int stationId, bool includeInactive = false);
}
