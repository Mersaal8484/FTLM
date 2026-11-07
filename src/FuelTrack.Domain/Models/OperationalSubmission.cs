using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Models;

/// <summary>One atomic operator upload for a Site (SubStation).</summary>
public sealed class OperationalSubmission : BaseEntity
{
    public int SubStationId { get; set; }
    public Guid ClientRequestId { get; set; }
    public DateTime? SubmissionCapturedAtUtc { get; set; }
    public DateTime? ClientSentAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public OperationalReadingStatus Status { get; set; } = OperationalReadingStatus.Pending;

    public SubStation SubStation { get; set; } = null!;
    public ICollection<OperationalGeneratorReading> GeneratorReadings { get; set; } = new List<OperationalGeneratorReading>();
    public ICollection<TankLevelReading> TankLevelReadings { get; set; } = new List<TankLevelReading>();
    public ICollection<FeederLoadReading> FeederLoadReadings { get; set; } = new List<FeederLoadReading>();
    public ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
}
