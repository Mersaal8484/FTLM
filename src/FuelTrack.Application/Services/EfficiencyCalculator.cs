using FuelTrack.Application.DTOs.Efficiency;

namespace FuelTrack.Application.Services;

public static class EfficiencyCalculator
{
    public const double TheoreticalFuelRateKWhPerLiter = 9.7;

    public static EfficiencyResultDto Calculate(double producedEnergyKWh, double consumedFuelLiters)
    {
        if (consumedFuelLiters <= 0)
            throw new ArgumentException("Consumed fuel must be greater than zero.", nameof(consumedFuelLiters));

        if (producedEnergyKWh < 0)
            throw new ArgumentException("Produced energy cannot be negative.", nameof(producedEnergyKWh));

        var efficiency = (producedEnergyKWh * 100.0) / (consumedFuelLiters * TheoreticalFuelRateKWhPerLiter);

        return new EfficiencyResultDto(producedEnergyKWh, consumedFuelLiters, efficiency);
    }
}
