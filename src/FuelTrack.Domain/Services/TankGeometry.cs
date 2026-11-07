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
}
