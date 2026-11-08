using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Models;

public sealed class OperationalGeneratorReading : BaseEntity
{
    public int OperationalSubmissionId { get; set; }
    public int GeneratorId { get; set; }
    public Guid ClientLineId { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateOnly BusinessDate { get; set; }
    public OperationalReadingStatus Status { get; set; } = OperationalReadingStatus.Pending;
    public bool IsRunning { get; set; }
    public decimal? LineVoltageVolts { get; set; }
    public decimal? CurrentAmps { get; set; }
    public decimal? WaterTemperatureCelsius { get; set; }
    public decimal? OilPressure { get; set; }
    public decimal? OilTemperatureCelsius { get; set; }
    public decimal? ActiveLoadKw { get; set; }
    public decimal? ApparentLoadKva { get; set; }
    public decimal? ReactiveLoadKvar { get; set; }
    public decimal? FrequencyHz { get; set; }
    public decimal? OilAddedLitres { get; set; }
    public decimal? PowerFactor { get; set; }
    public decimal? PeakLoadKw { get; set; }
    public decimal? AirPressureBar { get; set; }
    public string? VoidReason { get; set; }
    public string? WarningsJson { get; set; }

    // Phase 3 approval fields
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }
    public int? RejectedByUserId { get; set; }
    public string? RejectionReason { get; set; }

    public OperationalSubmission OperationalSubmission { get; set; } = null!;
    public Generator Generator { get; set; } = null!;
}
