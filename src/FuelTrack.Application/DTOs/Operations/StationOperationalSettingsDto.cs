namespace FuelTrack.Application.DTOs.Operations;

public sealed record StationOperationalSettingsDto(
    int StationId,
    string TimeZone,
    decimal ProducedVsSentWarningThresholdPercent,
    string OilPressureUnit,
    bool IsReadingPhotoRequired,
    int ReadingPhotoMaxBytes,
    bool RequiresDifferentReviewer);

public sealed record UpdateStationOperationalSettingsDto(
    string TimeZone,
    decimal ProducedVsSentWarningThresholdPercent,
    string OilPressureUnit,
    bool IsReadingPhotoRequired,
    int ReadingPhotoMaxBytes,
    bool RequiresDifferentReviewer);
