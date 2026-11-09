using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Services;

/// <summary>Pure tank geometry facade. The currently confirmed shape is vertical cylinder with conical bottom.</summary>
public static class TankGeometry
{
    private static readonly ITankGeometryStrategy VerticalConical = new VerticalConicalTankGeometryStrategy();

    public static decimal CalculateLitres(TankShape shape, decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor, decimal liquidLevelCm) => shape switch
    {
        TankShape.VerticalCylinderWithConicalBottom => VerticalConical.CalculateLitres(diameterCm, cylinderHeightCm, coneHeightCm, factor, liquidLevelCm),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), "No tank geometry strategy is registered for this shape."),
    };

    public static decimal CalculateLitres(decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor, decimal liquidLevelCm)
        => CalculateLitres(TankShape.VerticalCylinderWithConicalBottom, diameterCm, cylinderHeightCm, coneHeightCm, factor, liquidLevelCm);

    public static decimal CalculateCapacityLitres(decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor) =>
        CalculateLitres(diameterCm, cylinderHeightCm, coneHeightCm, factor, coneHeightCm + cylinderHeightCm);

    /// <summary>Derives the liquid height for a stored volume without persisting a duplicate calibration value.</summary>
    public static decimal CalculateLiquidLevelCm(decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor, decimal litres)
    {
        if (litres <= 0) return 0;
        var fullHeight = coneHeightCm + cylinderHeightCm;
        var capacity = CalculateCapacityLitres(diameterCm, cylinderHeightCm, coneHeightCm, factor);
        if (litres >= capacity) return fullHeight;

        var low = 0m;
        var high = fullHeight;
        for (var iteration = 0; iteration < 48; iteration++)
        {
            var mid = (low + high) / 2m;
            if (CalculateLitres(diameterCm, cylinderHeightCm, coneHeightCm, factor, mid) < litres) low = mid;
            else high = mid;
        }
        return decimal.Round((low + high) / 2m, 4, MidpointRounding.AwayFromZero);
    }
}
