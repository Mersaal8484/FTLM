namespace FuelTrack.Domain.Enums;

/// <summary>Lifecycle of an immutable operational line.</summary>
public enum OperationalReadingStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Recorded = 3,
    Voided = 4,
    AwaitingPhoto = 5
}
