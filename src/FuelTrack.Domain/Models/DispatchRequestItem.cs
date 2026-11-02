namespace FuelTrack.Domain.Models
{
    public class DispatchRequestItem : BaseEntity
    {
        public int DispatchRequestId { get; set; }
        public int SparePartId { get; set; }
        public decimal RequestedQuantity { get; set; }
        public decimal? ApprovedQuantity { get; set; }
        public decimal? IssuedQuantity { get; set; }
        public string? Notes { get; set; }
        public bool IsApproved { get; set; }

        public DispatchRequest DispatchRequest { get; set; } = null!;
        public SparePart SparePart { get; set; } = null!;
        public ICollection<InstalledPart> InstalledParts { get; set; } = new List<InstalledPart>();
    }
}
