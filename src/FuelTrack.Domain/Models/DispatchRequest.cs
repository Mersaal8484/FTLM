namespace FuelTrack.Domain.Models
{
    public class DispatchRequest : BaseEntity
    {
        public string RequestNumber { get; set; } = string.Empty;
        public int SubStationId { get; set; }
        public int StationId { get; set; }
        public DateTime RequestDate { get; set; }
        public int RequestedById { get; set; }
        public int? ApprovedById { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public int? WorkPermitId { get; set; }
        public int StatusLookupId { get; set; }
        public string? Purpose { get; set; }
        public string? Notes { get; set; }

        public SubStation SubStation { get; set; } = null!;
        public Station Station { get; set; } = null!;
        public User RequestedBy { get; set; } = null!;
        public User? ApprovedBy { get; set; }
        public WorkPermit? WorkPermit { get; set; }
        public Lookup StatusLookup { get; set; } = null!;
        public ICollection<DispatchRequestItem> Items { get; set; } = new List<DispatchRequestItem>();
    }
}
