namespace FuelTrack.Domain.Enums;

/// <summary>
/// Defines how a tank level in centimetres is converted to litres.
/// Geometry remains the backwards-compatible default for existing tanks.
/// </summary>
public enum TankCalibrationMode
{
    Geometry = 0,
    LitresPerCm = 1,
}
