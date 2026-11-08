namespace FuelTrack.Domain.Enums;

/// <summary>Explains why a tank level changed.  Only Level is routine monitoring data.</summary>
public enum TankEventType
{
    Level = 0,
    Receipt = 1,
    TransferOut = 2,
    TransferIn = 3
}
