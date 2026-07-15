using FuelTrack.Application.DTOs.SubStationReadings;

namespace FuelTrack.Application.Interfaces;

public interface ISubStationReadingService
{
    Task<SubStationReadingDto> CreateAsync(CreateSubStationReadingDto dto);
    Task<SubStationReadingDto> UpdateAsync(int id, CreateSubStationReadingDto dto);
    Task<SubStationReadingDto?> GetByIdAsync(int id);
    Task<List<SubStationReadingDto>> GetByStationAsync(int stationId);
    Task<SubStationReadingDto?> GetLastByStationAsync(int stationId, int? subStation = null);
    Task<List<PendingSubStationReadingDto>> GetPendingAsync();
    Task ApproveAsync(int id, int userId);
    Task RejectAsync(int id, int userId);
}
