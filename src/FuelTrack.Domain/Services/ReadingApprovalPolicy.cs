using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Services;

/// <summary>
/// The single policy boundary between monitoring records and official records.
/// Tier 1 is recorded immediately; Tier 2 must be approved.  A station may
/// deliberately promote Tier 1 to review, but no caller decides this ad hoc.
/// </summary>
public static class ReadingApprovalPolicy
{
    public static bool RequiresApproval(
        OperationalSubjectType subjectType,
        MeterReadingKind? kind = null,
        TankEventType? eventType = null,
        bool tier1RequiresApproval = false)
    {
        if (tier1RequiresApproval) return true;
        return subjectType switch
        {
            OperationalSubjectType.Meter => kind == MeterReadingKind.DailyClosing,
            OperationalSubjectType.Tank => eventType is TankEventType.Receipt or TankEventType.TransferIn or TankEventType.TransferOut,
            _ => false,
        };
    }

    public static OperationalReadingStatus InitialStatus(bool requiresApproval, bool awaitingPhoto) =>
        awaitingPhoto ? OperationalReadingStatus.AwaitingPhoto :
        requiresApproval ? OperationalReadingStatus.Pending : OperationalReadingStatus.Recorded;

    public static bool IsOfficial(OperationalReadingStatus status) =>
        status is OperationalReadingStatus.Recorded or OperationalReadingStatus.Approved;
}
