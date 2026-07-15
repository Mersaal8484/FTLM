namespace FuelTrack.Application.DTOs.Generators.My;

public record LastReadingDto(
    double? CurrentEnergyKWh,
    double? CurrentRunningHours,
    DateTime? ReadingDate,
    string? Source);
