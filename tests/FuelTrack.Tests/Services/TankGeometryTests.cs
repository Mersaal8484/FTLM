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

        Assert.Equal(decimal.Round(atConeTop / 8m, 4, MidpointRounding.AwayFromZero), atHalfCone);
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
}
