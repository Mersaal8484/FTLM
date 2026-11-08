using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Models;

/// <summary>Raw meter capture; official deltas are deliberately deferred to Phase 3 approval.</summary>
public sealed class MeterReading : BaseEntity
{
    public int OperationalSubmissionId { get; set; }
    public Guid ClientLineId { get; set; }
    public MeterReadingSubjectType SubjectType { get; set; }
    public int SubjectId { get; set; }
    public int? MeterId { get; set; }
    public MeterReadingKind Kind { get; set; } = MeterReadingKind.Periodic;
    public DateTime CapturedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateOnly BusinessDate { get; set; }
    public OperationalReadingStatus Status { get; set; } = OperationalReadingStatus.Pending;
    public decimal RawValue { get; set; }
    public decimal MultiplierSnapshot { get; set; } = 1m;
    public bool IsMeterReset { get; set; }
    public string? ResetNote { get; set; }
    public string? WarningsJson { get; set; }
    public string? VoidReason { get; set; }

    // Phase 3 approval fields
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }
    public int? RejectedByUserId { get; set; }
    public string? RejectionReason { get; set; }

    public OperationalSubmission OperationalSubmission { get; set; } = null!;
    public Meter? Meter { get; set; }
}
