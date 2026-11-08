using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Models;

/// <summary>Background-evaluated operating-limit episode; one notification is emitted when opened.</summary>
public sealed class OperatingLimitEpisode : BaseEntity
{
    public int GeneratorOperatingLimitId { get; set; }
    public int TriggerReadingId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public decimal ObservedValue { get; set; }
    public GeneratorOperatingLimit GeneratorOperatingLimit { get; set; } = null!;
    public OperationalGeneratorReading TriggerReading { get; set; } = null!;
}
