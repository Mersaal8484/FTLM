namespace FuelTrack.Domain.Models;

/// <summary>
/// Optional, operator-configured oil requirement for one maintenance item on one generator.
/// No system default is inferred: an absent record means the maintenance action must warn and
/// must not change oil stock.
/// </summary>
public sealed class GeneratorMaintenanceOilConfiguration : BaseEntity
{
    public int GeneratorId { get; set; }
    public int ServiceItemId { get; set; }
    public int OilId { get; set; }
    public decimal QuantityLiters { get; set; }

    public Generator Generator { get; set; } = null!;
    public ServiceItem ServiceItem { get; set; } = null!;
    public Oil Oil { get; set; } = null!;
}
