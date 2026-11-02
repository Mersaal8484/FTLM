namespace FuelTrack.Domain.Models
{
    public class WarehouseSupply : BaseEntity
    {
        public string SupplyNumber { get; set; } = string.Empty;
        public int StationId { get; set; }
        public DateTime SupplyDate { get; set; }
        public string? SupplierName { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public int StatusLookupId { get; set; }
        public string? Notes { get; set; }

        public Station Station { get; set; } = null!;
        public Lookup StatusLookup { get; set; } = null!;
        public ICollection<WarehouseSupplyItem> Items { get; set; } = new List<WarehouseSupplyItem>();
    }
}
