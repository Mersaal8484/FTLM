using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Models;

public sealed class FeederLoadReading : BaseEntity
{
    public int OperationalSubmissionId { get; set; }
    public int FeederId { get; set; }
    public Guid ClientLineId { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateOnly BusinessDate { get; set; }
    public OperationalReadingStatus Status { get; set; } = OperationalReadingStatus.Pending;
    public decimal? ActiveLoadKw { get; set; }
    public decimal? ApparentLoadKva { get; set; }
    public decimal MeterMultiplierSnapshot { get; set; }
    public string? WarningsJson { get; set; }
    public string? VoidReason { get; set; }

    // Phase 3 approval fields
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }
    public int? RejectedByUserId { get; set; }
    public string? RejectionReason { get; set; }

    public OperationalSubmission OperationalSubmission { get; set; } = null!;
    public Feeder Feeder { get; set; } = null!;
}
