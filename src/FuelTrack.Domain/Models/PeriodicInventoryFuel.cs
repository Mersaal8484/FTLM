namespace FuelTrack.Domain.Models
{
    public class PeriodicInventoryFuel
    {
        public int Id { get; set; }
        public int PeriodicInventoryId { get; set; }
        public int FuelLookupId { get; set; }
        public double ActualStockLiters { get; set; }

        public PeriodicInventory PeriodicInventory { get; set; } = null!;
        public Lookup FuelLookup { get; set; } = null!;
    }
}
