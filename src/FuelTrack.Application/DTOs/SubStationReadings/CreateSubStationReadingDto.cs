namespace FuelTrack.Application.DTOs.SubStationReadings;

public record CreateSubStationReadingDto(
    int SubStationId,
    int? MeterId,
    DateTime ReadingDate,
    double PrevExportedEnergyKWh,
    double CurrentExportedEnergyKWh,
    double? InternalConsumption,
    int? ReadingStatusLookupId,
    string? RejectionReason,
    string? Notes);
