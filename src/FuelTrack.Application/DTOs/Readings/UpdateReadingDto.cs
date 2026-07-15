namespace FuelTrack.Application.DTOs.Readings;

public record UpdateReadingDto(
    double? PrevEnergyKWh,
    double? CurrentEnergyKWh,
    double? PrevRunningHours,
    double? CurrentRunningHours,
    decimal? StandardFuelRate,
    int? OilLookupId,
    double? OilUsedLiters,
    string? Notes);
