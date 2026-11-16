namespace FuelTrack.Domain.Enums;

/// <summary>
/// Tank calibration shapes.
/// VerticalCylinderWithConicalBottom (0): enterprise / legacy vertical tanks.
/// HorizontalCylinderWithConicalEnds (1): commercial-mode main and daily tanks (owner design rule).
/// </summary>
public enum TankShape
{
    VerticalCylinderWithConicalBottom = 0,
    HorizontalCylinderWithConicalEnds = 1,
}
