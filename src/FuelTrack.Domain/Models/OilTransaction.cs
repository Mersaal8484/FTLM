using System.ComponentModel.DataAnnotations.Schema;

namespace FuelTrack.Domain.Models
{
    public class OilTransaction : BaseEntity
    {
        public int SubStationId { get; set; }
        public int OilLookupId { get; set; }
        public int TransactionTypeLookupId { get; set; }
        public DateTime TransactionDate { get; set; }
        public double QuantityLiters { get; set; }
        
        // -- بيانات الوارد --
        public string? Reference { get; set; }
        public string? SupplierCompany { get; set; }
        
        // -- النقل (صادر / وارد بين المواقع) --
        public int? SourceSubStationId { get; set; }
        public int? DestinationSubStationId { get; set; }

        // -- الاعتماد --
        public bool IsApproved { get; set; } = false;
        public int? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        // -- ملاحظات --
        public string? Notes { get; set; }

        // التصحيح
        public int? CorrectedFromId { get; set; }
        public string? CorrectionReason { get; set; }

        public SubStation SubStation { get; set; } = null!;
        public Lookup OilLookup { get; set; } = null!;
        public Lookup TransactionTypeLookup { get; set; } = null!;
        public SubStation? SourceSubStation { get; set; }
        public SubStation? DestinationSubStation { get; set; }
        public User? ApprovedBy { get; set; }
    }
}
