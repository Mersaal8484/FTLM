using FuelTrack.Application.DTOs.Intelligence;

namespace FuelTrack.Application.Interfaces;

public interface IAnalyticsInsightService
{
    Task<List<AnalyticsInsightDto>> GetInsightsAsync(int? subStationId, int? stationId);
    Task AddFeedbackAsync(CreateInsightFeedbackDto dto, int userId);
    Task GenerateInsightsAsync(int? subStationId, int? stationId);
}
