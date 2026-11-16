using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;

namespace FuelTrack.Tests.Services;

public sealed class HorizontalConicalTankGeometryTests
{
    private const TankShape Shape = TankShape.HorizontalCylinderWithConicalEnds;
    private const decimal Diameter = 200m;       // Radius R = 100 cm
    private const decimal BarrelLength = 300m;   // Cylindrical length (CylinderHeightCm)
    private const decimal ConeDepth = 40m;       // Axial depth of each conical end (ConeHeightCm)
    private const decimal Factor = 1m;

    [Fact]
    public void CalculateLitres_ReturnsZeroWhenEmpty()
    {
        var litres = TankGeometry.CalculateLitres(Shape, Diameter, BarrelLength, ConeDepth, Factor, 0m);
        Assert.Equal(0m, litres);
    }

    [Fact]
    public void CalculateLitres_FullTankEqualsCalculatedCapacity()
    {
        var capacity = TankGeometry.CalculateCapacityLitres(Shape, Diameter, BarrelLength, ConeDepth, Factor);
        var fullLitres = TankGeometry.CalculateLitres(Shape, Diameter, BarrelLength, ConeDepth, Factor, Diameter);

        Assert.Equal(capacity, fullLitres);
        Assert.True(capacity > 0);

        // Theoretical capacity:
        // Cylinder = pi * 100^2 * 300 / 1000 = 9424.778 L
        // 2 Cones   = 2 * (1/3 * pi * 100^2 * 40) / 1000 = 837.758 L
        // Total    ~ 10262.54 L
        Assert.InRange(capacity, 10250m, 10275m);
    }

    [Fact]
    public void CalculateLitres_HalfFullEqualsHalfCapacityBySymmetry()
    {
        var capacity = TankGeometry.CalculateCapacityLitres(Shape, Diameter, BarrelLength, ConeDepth, Factor);
        var halfLitres = TankGeometry.CalculateLitres(Shape, Diameter, BarrelLength, ConeDepth, Factor, Diameter / 2m);

        // A horizontal symmetric cylinder + symmetric conical ends must be exactly 50% full at R.
        Assert.Equal(decimal.Round(capacity / 2m, 2), decimal.Round(halfLitres, 2));
    }

    [Fact]
    public void CalculateLitres_IsMonotonicallyIncreasing()
    {
        var prev = 0m;
        for (var level = 10m; level <= Diameter; level += 20m)
        {
            var litres = TankGeometry.CalculateLitres(Shape, Diameter, BarrelLength, ConeDepth, Factor, level);
            Assert.True(litres > prev, $"Expected volume at {level} cm to be greater than at {level - 20m} cm.");
            prev = litres;
        }
    }

    [Fact]
    public void CalculateLitres_AppliesMultiplicationFactor()
    {
        var normal = TankGeometry.CalculateLitres(Shape, Diameter, BarrelLength, ConeDepth, 1m, 100m);
        var scaled = TankGeometry.CalculateLitres(Shape, Diameter, BarrelLength, ConeDepth, 1.15m, 100m);

        Assert.Equal(decimal.Round(normal * 1.15m, 2), decimal.Round(scaled, 2));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(201)]
    public void CalculateLitres_RejectsLevelOutsideDiameter(decimal level)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TankGeometry.CalculateLitres(Shape, Diameter, BarrelLength, ConeDepth, Factor, level));
    }

    [Theory]
    [InlineData(20)]
    [InlineData(60)]
    [InlineData(100)]
    [InlineData(150)]
    [InlineData(190)]
    public void CalculateLiquidLevelCm_InvertsVolumeAccurately(decimal originalLevelCm)
    {
        var litres = TankGeometry.CalculateLitres(Shape, Diameter, BarrelLength, ConeDepth, Factor, originalLevelCm);
        var derivedLevel = TankGeometry.CalculateLiquidLevelCm(Shape, Diameter, BarrelLength, ConeDepth, Factor, litres);

        Assert.Equal(originalLevelCm, derivedLevel, 1);
    }

    [Fact]
    public void TankVolumeService_ResolvesDeadStockForHorizontalConicalTank()
    {
        var deadStockCm = 15m;
        var tank = new FuelTank
        {
            Shape = Shape,
            DiameterCm = Diameter,
            CylinderHeightCm = BarrelLength,
            ConeHeightCm = ConeDepth,
            MultiplicationFactor = 1m,
            DeadStockLevelCm = deadStockCm,
            DeadStockInputUnit = "cm",
            DeadStockLiters = null
        };

        var litres = TankVolumeService.DeadStockLitres(tank);
        var expected = TankGeometry.CalculateLitres(Shape, Diameter, BarrelLength, ConeDepth, 1m, deadStockCm);

        Assert.True(litres > 0);
        Assert.Equal(expected, litres);
    }
}
