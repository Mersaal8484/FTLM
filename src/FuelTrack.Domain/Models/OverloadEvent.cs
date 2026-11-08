namespace FuelTrack.Domain.Models;

/// <summary>One open event represents one continuous overload episode for a generator.</summary>
public sealed class OverloadEvent : BaseEntity
{
    public int GeneratorId { get; set; }
    public int TriggerReadingId { get; set; }
    public int LatestReadingId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public decimal PeakPercent { get; set; }
    public int ConsecutiveNormalReadings { get; set; }
    public DateTime? LatestReadingCapturedAtUtc { get; set; }
    public Generator Generator { get; set; } = null!;
    public OperationalGeneratorReading TriggerReading { get; set; } = null!;
}
