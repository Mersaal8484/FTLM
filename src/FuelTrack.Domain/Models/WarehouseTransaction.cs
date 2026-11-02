namespace FuelTrack.Domain.Models
{
    public class WarehouseTransaction : BaseEntity
    {
        public int SparePartId { get; set; }
        public int StationId { get; set; }
        public int TransactionTypeLookupId { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Quantity { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public int? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public string? Notes { get; set; }

        public SparePart SparePart { get; set; } = null!;
        public Station Station { get; set; } = null!;
        public Lookup TransactionTypeLookup { get; set; } = null!;
    }
}
