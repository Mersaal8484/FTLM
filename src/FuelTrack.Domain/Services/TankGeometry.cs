using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Services;

/// <summary>
/// Pure tank geometry facade.
/// Registered shapes:
///   VerticalCylinderWithConicalBottom (0) — vertical cylinder with conical bottom.
/// </summary>
public static class TankGeometry
{
    private static readonly ITankGeometryStrategy VerticalConical = new VerticalConicalTankGeometryStrategy();

    private static ITankGeometryStrategy StrategyFor(TankShape shape) => shape switch
    {
        TankShape.VerticalCylinderWithConicalBottom => VerticalConical,
        _ => throw new ArgumentOutOfRangeException(nameof(shape), "No tank geometry strategy is registered for this shape."),
    };

    public static decimal CalculateLitres(TankShape shape, decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor, decimal liquidLevelCm)
        => StrategyFor(shape).CalculateLitres(diameterCm, cylinderHeightCm, coneHeightCm, factor, liquidLevelCm);

    /// <summary>Convenience overload — defaults to <see cref="TankShape.VerticalCylinderWithConicalBottom"/> (enterprise path).</summary>
    public static decimal CalculateLitres(decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor, decimal liquidLevelCm)
        => CalculateLitres(TankShape.VerticalCylinderWithConicalBottom, diameterCm, cylinderHeightCm, coneHeightCm, factor, liquidLevelCm);

    /// <summary>Returns the full capacity of the tank in litres (liquid level = top).</summary>
    public static decimal CalculateCapacityLitres(TankShape shape, decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor)
    {
        decimal fullLevel = coneHeightCm + cylinderHeightCm;
        return CalculateLitres(shape, diameterCm, cylinderHeightCm, coneHeightCm, factor, fullLevel);
    }

    /// <summary>Convenience overload — defaults to <see cref="TankShape.VerticalCylinderWithConicalBottom"/>.</summary>
    public static decimal CalculateCapacityLitres(decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor) =>
        CalculateCapacityLitres(TankShape.VerticalCylinderWithConicalBottom, diameterCm, cylinderHeightCm, coneHeightCm, factor);

    /// <summary>Derives the liquid height (cm) for a stored volume using bisection — no duplicate calibration value stored.</summary>
    public static decimal CalculateLiquidLevelCm(TankShape shape, decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor, decimal litres)
    {
        if (litres <= 0) return 0;
        decimal fullLevel = coneHeightCm + cylinderHeightCm;
        var capacity = CalculateCapacityLitres(shape, diameterCm, cylinderHeightCm, coneHeightCm, factor);
        if (litres >= capacity) return fullLevel;

        var low = 0m;
        var high = fullLevel;
        for (var iteration = 0; iteration < 48; iteration++)
        {
            var mid = (low + high) / 2m;
            if (CalculateLitres(shape, diameterCm, cylinderHeightCm, coneHeightCm, factor, mid) < litres) low = mid;
            else high = mid;
        }
        return decimal.Round((low + high) / 2m, 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>Convenience overload — defaults to <see cref="TankShape.VerticalCylinderWithConicalBottom"/>.</summary>
    public static decimal CalculateLiquidLevelCm(decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor, decimal litres)
        => CalculateLiquidLevelCm(TankShape.VerticalCylinderWithConicalBottom, diameterCm, cylinderHeightCm, coneHeightCm, factor, litres);
}
