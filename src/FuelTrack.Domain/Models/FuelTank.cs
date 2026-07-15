namespace FuelTrack.Domain.Models
{
    public class FuelTank : BaseEntity
    {
        public int SubStationId { get; set; }
        public int FuelLookupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public double CapacityLiters { get; set; }
        public double? MinAlertLiters { get; set; }
        public double? DeadStockLiters { get; set; }
        public double? ActualStockLiters { get; set; }
        public string? Notes { get; set; }

        public SubStation SubStation { get; set; } = null!;
        public Lookup FuelLookup { get; set; } = null!;
    }
}
