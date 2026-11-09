using FuelTrack.Domain.Services;

namespace FuelTrack.Tests.Services;

public sealed class TankGeometryTests
{
    private const decimal Diameter = 200m;
    private const decimal CylinderHeight = 150m;
    private const decimal ConeHeight = 50m;

    [Fact]
    public void CalculateLitres_ReturnsZeroAtConeApex() =>
        Assert.Equal(0m, TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, 0m));

    [Fact]
    public void CalculateLitres_UsesCubicVolumeWithinCone()
    {
        var atHalfCone = TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, 25m);
        var atConeTop = TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, 50m);

        // Cube law: volume at h = volume at 2h / 8. Compared at 2 d.p. (brief tolerance ±0.01 L).
        Assert.Equal(decimal.Round(atConeTop / 8m, 2), decimal.Round(atHalfCone, 2));
    }

    [Fact]
    public void CalculateLitres_IsContinuousAtConeCylinderJunction()
    {
        var topOfCone = TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, 50m);
        var justAbove = TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, 50.0001m);

        Assert.True(justAbove >= topOfCone);
    }

    [Fact]
    public void CalculateLitres_AtFullLevelEqualsCalculatedCapacity() =>
        Assert.Equal(
            TankGeometry.CalculateCapacityLitres(Diameter, CylinderHeight, ConeHeight, 1m),
            TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, 200m));

    [Fact]
    public void CalculateLitres_AppliesFactor() =>
        Assert.Equal(
            TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, 100m) * 1.1m,
            TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1.1m, 100m),
            3);

    [Theory]
    [InlineData(-1)]
    [InlineData(201)]
    public void CalculateLitres_RejectsLevelOutsideTank(decimal level) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, level));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(25, 65.45)]
    [InlineData(50, 523.60)]
    [InlineData(100, 2094.40)]
    [InlineData(200, 5235.99)]
    public void RequiredVerticalConeFixtures_AreAccurate(decimal level, decimal expected) =>
        Assert.Equal(expected, TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, level), 2);

    [Fact]
    public void RequiredFactorFixture_IsAccurate() =>
        Assert.Equal(2303.83m, TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1.1m, 100m), 2);

    [Theory]
    [InlineData(25, 65.45)]
    [InlineData(50, 523.60)]
    [InlineData(100, 2094.40)]
    [InlineData(200, 5235.99)]
    public void CalculateLiquidLevelCm_DerivedFromLitres_MatchesOriginalLevel(decimal originalLevelCm, decimal expectedLitres)
    {
        var derivedLevel = TankGeometry.CalculateLiquidLevelCm(Diameter, CylinderHeight, ConeHeight, 1m, expectedLitres);
        Assert.Equal(originalLevelCm, derivedLevel, 1);
    }

    [Fact]
    public void DeadStockInput_InCmOrLitres_ProducesEquivalentCalculatedVolume()
    {
        var deadStockCm = 25m;
        var litresFromCm = TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, deadStockCm);
        var cmFromLitres = TankGeometry.CalculateLiquidLevelCm(Diameter, CylinderHeight, ConeHeight, 1m, litresFromCm);

        Assert.Equal(65.45m, litresFromCm, 2);
        Assert.Equal(deadStockCm, cmFromLitres, 2);
    }
}
