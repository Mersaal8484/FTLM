namespace FuelTrack.Application.DTOs.Readings;

public record CreateReadingDto(
    int GeneratorId,
    DateTime ReadingDate,
    double PrevEnergyKWh,
    double CurrentEnergyKWh,
    double PrevRunningHours,
    double CurrentRunningHours,
    int FuelLookupId,
    decimal StandardFuelRate,
    int OilLookupId,
    double? OilUsedLiters,
    string? Notes);
