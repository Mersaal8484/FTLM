using FuelTrack.Application.Services;

namespace FuelTrack.Tests.Services;

public class EfficiencyCalculatorTests
{
    [Theory]
    [InlineData(9409, 970, 100)]
    [InlineData(9409, 1940, 50)]
    [InlineData(9409, 485, 200)]
    public void Calculate_ReturnsExpectedEfficiency(double energyKWh, double fuelLiters, double expectedPercent)
    {
        var result = EfficiencyCalculator.Calculate(energyKWh, fuelLiters);

        Assert.Equal(energyKWh, result.ProducedEnergyKWh);
        Assert.Equal(fuelLiters, result.ConsumedFuelLiters);
        Assert.Equal(expectedPercent, result.EfficiencyPercent, 1);
    }

    [Fact]
    public void Calculate_WhenFuelIsZero_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            EfficiencyCalculator.Calculate(1000, 0));

        Assert.Contains("greater than zero", ex.Message);
    }

    [Fact]
    public void Calculate_WhenEnergyIsNegative_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            EfficiencyCalculator.Calculate(-1, 100));

        Assert.Contains("cannot be negative", ex.Message);
    }

    [Fact]
    public void Calculate_ValuesOver100Percent_AreAccepted()
    {
        var result = EfficiencyCalculator.Calculate(10000, 500);

        Assert.True(result.EfficiencyPercent > 100);
    }
}
