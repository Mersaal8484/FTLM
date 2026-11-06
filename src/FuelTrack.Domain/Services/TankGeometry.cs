namespace FuelTrack.Domain.Services;

/// <summary>Pure approved vertical-cylinder-with-conical-bottom tank calculation, in centimetres.</summary>
public static class TankGeometry
{
    public static decimal CalculateLitres(decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor, decimal liquidLevelCm)
    {
        if (diameterCm <= 0 || cylinderHeightCm < 0 || coneHeightCm <= 0 || factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(diameterCm), "Tank calibration values must be positive (cylinder height may be zero).");
        if (liquidLevelCm < 0 || liquidLevelCm > coneHeightCm + cylinderHeightCm)
            throw new ArgumentOutOfRangeException(nameof(liquidLevelCm), "Liquid level is outside the calibrated tank range.");

        var r = diameterCm / 2m;
        var pi = (decimal)Math.PI;
        var coneVolume = pi * r * r * coneHeightCm / 3m;
        decimal volume;
        if (liquidLevelCm <= coneHeightCm)
            volume = pi * r * r * liquidLevelCm * liquidLevelCm * liquidLevelCm / (3m * coneHeightCm * coneHeightCm);
        else
            volume = coneVolume + pi * r * r * (liquidLevelCm - coneHeightCm);

        return decimal.Round(volume * factor / 1000m, 4, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateCapacityLitres(decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor) =>
        CalculateLitres(diameterCm, cylinderHeightCm, coneHeightCm, factor, coneHeightCm + cylinderHeightCm);
}
