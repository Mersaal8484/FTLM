namespace FuelTrack.Application.DTOs.Intelligence;

public record AnalyticsInsightDto(
    int Id,
    int? SubStationId,
    int? StationId,
    string Title,
    string Description,
    string InsightType,
    string? Severity,
    string? Category,
    string? DataPayload,
    DateTime GeneratedAt);
