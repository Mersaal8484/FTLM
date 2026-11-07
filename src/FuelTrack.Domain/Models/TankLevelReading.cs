using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Models;

/// <summary>Immutable tank-level observation and calibration snapshot.</summary>
public sealed class TankLevelReading : BaseEntity
{
    public int OperationalSubmissionId { get; set; }
    public int FuelTankId { get; set; }
    public Guid ClientLineId { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateOnly BusinessDate { get; set; }
    public OperationalReadingStatus Status { get; set; } = OperationalReadingStatus.Pending;
    public decimal LiquidLevelCm { get; set; }
    public decimal ComputedLitres { get; set; }
    public TankEventType EventType { get; set; } = TankEventType.Level;
    public int? LinkedEventId { get; set; }
    public TankLevelReading? LinkedEvent { get; set; }
    public decimal? BeforeLiquidLevelCm { get; set; }
    public decimal? BeforeComputedLitres { get; set; }
    public decimal? DeliveryNoteLitres { get; set; }
    public decimal DeadStockLitresSnapshot { get; set; }
    public string? VoidReason { get; set; }
    public TankShape ShapeSnapshot { get; set; }
    public decimal DiameterCmSnapshot { get; set; }
    public decimal CylinderHeightCmSnapshot { get; set; }
    public decimal ConeHeightCmSnapshot { get; set; }
    public decimal MultiplicationFactorSnapshot { get; set; }
    public string? WarningsJson { get; set; }

    // Phase 3 approval fields
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }
    public int? RejectedByUserId { get; set; }
    public string? RejectionReason { get; set; }

    public OperationalSubmission OperationalSubmission { get; set; } = null!;
    public FuelTank FuelTank { get; set; } = null!;
}
