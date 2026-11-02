namespace FuelTrack.Domain.Models
{
    public class PeriodicInventoryOil
    {
        public int Id { get; set; }
        public int PeriodicInventoryId { get; set; }
        public int OilLookupId { get; set; }
        public int? OilId { get; set; }
        public double ActualStockLiters { get; set; }

        public PeriodicInventory PeriodicInventory { get; set; } = null!;
        public Lookup OilLookup { get; set; } = null!;
        public Oil? Oil { get; set; }
    }
}
