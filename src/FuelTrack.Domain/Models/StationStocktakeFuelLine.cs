namespace FuelTrack.Domain.Models;

/// <summary>Immutable per-tank snapshots captured with a stocktake.</summary>
public sealed class StationStocktakeFuelLine : BaseEntity
{
    public int StocktakeId { get; set; }
    public int FuelTankId { get; set; }
    public decimal CountedLiters { get; set; }
    public decimal BookLiters { get; set; }
    public decimal DeadStockLiters { get; set; }
    public decimal UsableCountedLiters { get; set; }
    public decimal VarianceLiters { get; set; }
    public decimal? DipLevelCm { get; set; }

    public StationStocktake Stocktake { get; set; } = null!;
    public FuelTank FuelTank { get; set; } = null!;
}
