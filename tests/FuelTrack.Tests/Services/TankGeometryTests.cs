using FuelTrack.Domain.Services;

namespace FuelTrack.Tests.Services;

public sealed class TankGeometryTests
{
    private const decimal Diameter = 200m;
    private const decimal CylinderHeight = 300m;
    private const decimal ConeHeight = 100m;

    [Fact]
    public void CalculateLitres_ReturnsZeroAtConeApex() =>
        Assert.Equal(0m, TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, 0m));

    [Fact]
    public void CalculateLitres_UsesCubicVolumeWithinCone()
    {
        var atHalfCone = TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, 50m);
        var atConeTop = TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, 100m);

        Assert.Equal(decimal.Round(atConeTop / 8m, 4, MidpointRounding.AwayFromZero), atHalfCone);
    }

    [Fact]
    public void CalculateLitres_IsContinuousAtConeCylinderJunction()
    {
        var topOfCone = TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, 100m);
        var justAbove = TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, 100.0001m);

        Assert.True(justAbove >= topOfCone);
    }

    [Fact]
    public void CalculateLitres_AtFullLevelEqualsCalculatedCapacity() =>
        Assert.Equal(
            TankGeometry.CalculateCapacityLitres(Diameter, CylinderHeight, ConeHeight, 1m),
            TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, 400m));

    [Fact]
    public void CalculateLitres_AppliesFactor() =>
        Assert.Equal(
            TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, 250m) * 1.1m,
            TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1.1m, 250m),
            3);

    [Theory]
    [InlineData(-1)]
    [InlineData(401)]
    public void CalculateLitres_RejectsLevelOutsideTank(decimal level) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TankGeometry.CalculateLitres(Diameter, CylinderHeight, ConeHeight, 1m, level));
}
