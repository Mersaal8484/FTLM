using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Services;

public sealed class VerticalConicalTankGeometryStrategy : ITankGeometryStrategy
{
    public TankShape Shape => TankShape.VerticalCylinderWithConicalBottom;

    public decimal CalculateLitres(decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor, decimal liquidLevelCm)
    {
        if (diameterCm <= 0 || cylinderHeightCm < 0 || coneHeightCm <= 0 || factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(diameterCm), "Tank calibration values must be positive (cylinder height may be zero).");
        if (liquidLevelCm < 0 || liquidLevelCm > coneHeightCm + cylinderHeightCm)
            throw new ArgumentOutOfRangeException(nameof(liquidLevelCm), "Liquid level is outside the calibrated tank range.");
        var radius = diameterCm / 2m;
        var pi = (decimal)Math.PI;
        var coneVolume = pi * radius * radius * coneHeightCm / 3m;
        var volume = liquidLevelCm <= coneHeightCm
            ? pi * radius * radius * liquidLevelCm * liquidLevelCm * liquidLevelCm / (3m * coneHeightCm * coneHeightCm)
            : coneVolume + pi * radius * radius * (liquidLevelCm - coneHeightCm);
        return decimal.Round(volume * factor / 1000m, 4, MidpointRounding.AwayFromZero);
    }
}
