using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Models;

/// <summary>
/// A submitted physical stocktake. Scope identifiers are assigned by the server;
/// commercial clients never provide them.
/// </summary>
public sealed class StationStocktake : BaseEntity
{
    public int StationId { get; set; }
    public int SubStationId { get; set; }
    public DateOnly StocktakeDate { get; set; }
    public StocktakeStatus Status { get; set; } = StocktakeStatus.Pending;
    public int SubmittedByUserId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
    public Guid ClientRequestId { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    public Station Station { get; set; } = null!;
    public SubStation SubStation { get; set; } = null!;
    public User SubmittedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public ICollection<StationStocktakeFuelLine> FuelLines { get; set; } = new List<StationStocktakeFuelLine>();
    public ICollection<StationStocktakeOilLine> OilLines { get; set; } = new List<StationStocktakeOilLine>();
}
