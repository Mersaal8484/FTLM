using FuelTrack.Domain.Models;

namespace FuelTrack.Domain.Services;

public static class TankVolumeService
{
    public static decimal UsableLitres(decimal measuredLitres, decimal deadStockLitres) =>
        Math.Max(0m, measuredLitres - deadStockLitres);

    public static bool IsAtOrBelowDeadStock(decimal measuredLitres, decimal deadStockLitres) =>
        measuredLitres <= deadStockLitres;

    /// <summary>Derives litres when centimetres were the operator-entered source of truth.</summary>
    public static decimal DeadStockLitres(FuelTank tank)
    {
        if (string.Equals(tank.DeadStockInputUnit, "cm", StringComparison.OrdinalIgnoreCase)
            && tank.DeadStockLevelCm.HasValue
            && tank.DiameterCm.HasValue && tank.CylinderHeightCm.HasValue
            && tank.ConeHeightCm.HasValue && tank.MultiplicationFactor.HasValue)
        {
            return TankGeometry.CalculateLitres(tank.DiameterCm.Value, tank.CylinderHeightCm.Value,
                tank.ConeHeightCm.Value, tank.MultiplicationFactor.Value, tank.DeadStockLevelCm.Value);
        }
        return (decimal)(tank.DeadStockLiters ?? 0d);
    }
}
