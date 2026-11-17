namespace FuelTrack.Domain.Enums;

/// <summary>
/// Tank calibration shapes.
/// VerticalCylinderWithConicalBottom (0): vertical cylinder with conical bottom (commercial and enterprise default).
/// HorizontalCylinderWithConicalEnds (1): obsolete — owner confirmed commercial tanks are vertical with conical bottom.
/// </summary>
public enum TankShape
{
    VerticalCylinderWithConicalBottom = 0,
    [Obsolete("Not used by any station mode")]
    HorizontalCylinderWithConicalEnds = 1,
}

