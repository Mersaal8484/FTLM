namespace FuelTrack.Application.DTOs.Readings;

public record PendingReadingDto(
    int Id,
    string GeneratorName,
    string? StationName,
    string? SubStationName,
    DateTime ReadingDate,
    double PrevEnergyKWh,
    double CurrentEnergyKWh,
    double FuelConsumedLiters,
    double OilUsedLiters,
    string? OilLookupName,
    string? CreatedByName);
