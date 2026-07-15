namespace FuelTrack.Application.DTOs.Efficiency;

public record EfficiencyResultDto(
    double ProducedEnergyKWh,
    double ConsumedFuelLiters,
    double EfficiencyPercent);
