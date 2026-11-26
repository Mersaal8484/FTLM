using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;

namespace FuelTrack.Tests.Services;

public sealed class TankLevelConverterTests
{
    [Fact]
    public void Geometry_ConvertsBothDirections_WithThreeDecimalRounding()
    {
        var tank = new FuelTank
        {
            CalibrationMode = TankCalibrationMode.Geometry,
            DiameterCm = 200m,
            ConeHeightCm = 50m,
            CylinderHeightCm = 150m,
            MultiplicationFactor = 1m,
        };

        var litres = TankLevelConverter.LitresFromLevel(tank, 100m);

        Assert.Equal(2094.395m, litres);
        Assert.Equal(100m, TankLevelConverter.LevelFromLitres(tank, litres));
    }

    [Fact]
    public void Linear_ConvertsBothDirections_WithThreeDecimalRounding()
    {
        var tank = new FuelTank { CalibrationMode = TankCalibrationMode.LitresPerCm, LitresPerCm = 10.125m };

        Assert.Equal(1012.5m, TankLevelConverter.LitresFromLevel(tank, 100m));
        Assert.Equal(100m, TankLevelConverter.LevelFromLitres(tank, 1012.5m));
    }

    [Fact]
    public void MaxLevelCm_And_RangeValidation_RejectAboveCapacityForLinearAndGeometryModes()
    {
        var linearTank = new FuelTank
        {
            CalibrationMode = TankCalibrationMode.LitresPerCm,
            LitresPerCm = 10m,
            CapacityLiters = 1000d,
        };
        var geometryTank = new FuelTank
        {
            CalibrationMode = TankCalibrationMode.Geometry,
            DiameterCm = 200m,
            ConeHeightCm = 50m,
            CylinderHeightCm = 150m,
            MultiplicationFactor = 1m,
        };

        Assert.Equal(100m, TankLevelConverter.MaxLevelCm(linearTank));
        Assert.Equal(200m, TankLevelConverter.MaxLevelCm(geometryTank));

        Assert.Throws<ArgumentOutOfRangeException>(() => TankLevelConverter.LitresFromLevel(linearTank, -1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => TankLevelConverter.LitresFromLevel(linearTank, 100.1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => TankLevelConverter.LitresFromLevel(geometryTank, 200.1m));
    }
}
