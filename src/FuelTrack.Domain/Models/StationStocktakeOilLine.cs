namespace FuelTrack.Domain.Models;

/// <summary>Immutable per-oil snapshots captured with a stocktake.</summary>
public sealed class StationStocktakeOilLine : BaseEntity
{
    public int StocktakeId { get; set; }
    public int OilId { get; set; }
    public int OilLookupId { get; set; }
    public decimal CountedLiters { get; set; }
    public decimal BookLiters { get; set; }
    public decimal VarianceLiters { get; set; }

    public StationStocktake Stocktake { get; set; } = null!;
    public Oil Oil { get; set; } = null!;
    public Lookup OilLookup { get; set; } = null!;
}
