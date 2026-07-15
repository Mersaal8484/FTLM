namespace FuelTrack.Application.DTOs.SubStationReadings;

public record SubStationReadingDto(
    int Id,
    int SubStationId,
    string? SubStationName,
    int? MeterId,
    string? MeterName,
    DateTime ReadingDate,
    double PrevExportedEnergyKWh,
    double CurrentExportedEnergyKWh,
    double? InternalConsumption,
    int ReadingStatusLookupId,
    string? ReadingStatusLookupName,
    string? RejectionReason,
    string? Notes,
    DateTime CreatedAt,
    int CreatedBy);

public record PendingSubStationReadingDto(
    int Id,
    int SubStationId,
    string? SubStationName,
    int? MeterId,
    string? MeterName,
    DateTime ReadingDate,
    double PrevExportedEnergyKWh,
    double CurrentExportedEnergyKWh,
    double? InternalConsumption,
    int ReadingStatusLookupId,
    string? ReadingStatusLookupName,
    string? RejectionReason,
    string? Notes,
    DateTime CreatedAt,
    int CreatedBy);
