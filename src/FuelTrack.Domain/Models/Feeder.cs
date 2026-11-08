namespace FuelTrack.Domain.Models;

public sealed class Feeder : BaseEntity
{
    public int SubStationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MeterMultiplier { get; set; } = 1m;
    public int? MeterId { get; set; }

    public SubStation SubStation { get; set; } = null!;
    public Meter? Meter { get; set; }
    public ICollection<FeederLoadReading> LoadReadings { get; set; } = new List<FeederLoadReading>();
}
