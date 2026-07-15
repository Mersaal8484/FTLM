namespace FuelTrack.Application.DTOs.Intelligence;

public record CreateInsightFeedbackDto(
    int AnalyticsInsightId,
    bool IsHelpful,
    string? Comment);
