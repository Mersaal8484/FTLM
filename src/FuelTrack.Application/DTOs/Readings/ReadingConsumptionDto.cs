namespace FuelTrack.Application.DTOs.Readings;

public record ReadingConsumptionDto(
    double PrevRunningHours,
    double CurrentRunningHours,
    double DeltaRunningHours,
    double PrevEnergyKWh,
    double CurrentEnergyKWh,
    double DeltaEnergyKWh,
    double StandardFuelRate,
    double EstimatedFuelConsumedLiters);
