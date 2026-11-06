using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Models;

/// <summary>Immutable tank-level observation and calibration snapshot.</summary>
public sealed class TankLevelReading : BaseEntity
{
    public int OperationalSubmissionId { get; set; }
    public int FuelTankId { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateOnly BusinessDate { get; set; }
    public OperationalReadingStatus Status { get; set; } = OperationalReadingStatus.Pending;
    public decimal LiquidLevelCm { get; set; }
    public decimal ComputedLitres { get; set; }
    public TankShape ShapeSnapshot { get; set; }
    public decimal DiameterCmSnapshot { get; set; }
    public decimal CylinderHeightCmSnapshot { get; set; }
    public decimal ConeHeightCmSnapshot { get; set; }
    public decimal MultiplicationFactorSnapshot { get; set; }
    public string? WarningsJson { get; set; }

    public OperationalSubmission OperationalSubmission { get; set; } = null!;
    public FuelTank FuelTank { get; set; } = null!;
}
