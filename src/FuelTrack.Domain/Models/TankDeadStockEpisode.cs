namespace FuelTrack.Domain.Models;

/// <summary>
/// Open/closed episode for a daily tank at or below its configured dead stock.
/// The episode, rather than a time window, guarantees one alert per occurrence.
/// </summary>
public sealed class TankDeadStockEpisode : BaseEntity
{
    public int FuelTankId { get; set; }
    public int TriggerReadingId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }

    public FuelTank FuelTank { get; set; } = null!;
    public TankLevelReading TriggerReading { get; set; } = null!;
}
