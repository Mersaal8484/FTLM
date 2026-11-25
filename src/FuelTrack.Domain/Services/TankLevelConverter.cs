using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;

namespace FuelTrack.Domain.Services;

/// <summary>Single authoritative conversion path for tank litres and liquid level.</summary>
public static class TankLevelConverter
{
    public static bool IsConfigured(FuelTank tank) => tank.CalibrationMode switch
    {
        TankCalibrationMode.LitresPerCm => tank.LitresPerCm is > 0,
        _ => tank.DiameterCm is > 0 && tank.CylinderHeightCm is >= 0 && tank.ConeHeightCm is > 0 && tank.MultiplicationFactor is > 0,
    };

    public static decimal LitresFromLevel(FuelTank tank, decimal levelCm)
    {
        if (levelCm < 0) throw new ArgumentOutOfRangeException(nameof(levelCm));
        return tank.CalibrationMode switch
        {
            TankCalibrationMode.LitresPerCm when tank.LitresPerCm is > 0 => Round(levelCm * tank.LitresPerCm.Value),
            TankCalibrationMode.LitresPerCm => throw new InvalidOperationException("Litres-per-centimetre calibration is required."),
            _ when IsConfigured(tank) => Round(TankGeometry.CalculateLitres(
                tank.Shape ?? TankShape.VerticalCylinderWithConicalBottom,
                tank.DiameterCm!.Value,
                tank.CylinderHeightCm!.Value,
                tank.ConeHeightCm!.Value,
                tank.MultiplicationFactor!.Value,
                levelCm)),
            _ => throw new InvalidOperationException("Tank geometry calibration is required."),
        };
    }

    public static decimal LevelFromLitres(FuelTank tank, decimal litres)
    {
        if (litres < 0) throw new ArgumentOutOfRangeException(nameof(litres));
        return tank.CalibrationMode switch
        {
            TankCalibrationMode.LitresPerCm when tank.LitresPerCm is > 0 => Round(litres / tank.LitresPerCm.Value),
            TankCalibrationMode.LitresPerCm => throw new InvalidOperationException("Litres-per-centimetre calibration is required."),
            _ when IsConfigured(tank) => TankGeometry.CalculateLiquidLevelCm(
                tank.Shape ?? TankShape.VerticalCylinderWithConicalBottom,
                tank.DiameterCm!.Value,
                tank.CylinderHeightCm!.Value,
                tank.ConeHeightCm!.Value,
                tank.MultiplicationFactor!.Value,
                litres),
            _ => throw new InvalidOperationException("Tank geometry calibration is required."),
        };
    }

    public static decimal Round(decimal value) => decimal.Round(value, 3, MidpointRounding.AwayFromZero);
}
