namespace FuelTrack.Domain.Enums;

/// <summary>Lifecycle of a captured operational line. Phase 2 writes Pending only.</summary>
public enum OperationalReadingStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
