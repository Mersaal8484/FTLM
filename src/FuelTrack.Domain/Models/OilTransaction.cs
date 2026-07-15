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
        
        // -- بيانات الوارد المعقدة --
        public string? SupplierCompany { get; set; }
        public string? DriverName { get; set; }
        public string? DriverIdCard { get; set; }
        public string? TruckNumber { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public double? InvoiceQuantity { get; set; }
        public string? WarehouseReceiptNumber { get; set; }
        public DateTime? WarehouseReceiptDate { get; set; }
        public double? WarehouseQuantity { get; set; }
        
        // -- النقل (صادر / وارد بين المواقع) --
        public int? SourceSubStationId { get; set; }         
        public int? DestinationSubStationId { get; set; }    

        // الاعتماد المزدوج
        public bool IsApproved { get; set; } = false;        
        public int? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        public string? Notes { get; set; }
        
        public int? OilId { get; set; }

        public SubStation SubStation { get; set; } = null!;
        public Lookup OilLookup { get; set; } = null!;
        public Lookup TransactionTypeLookup { get; set; } = null!;
        public SubStation? SourceSubStation { get; set; }
        public SubStation? DestinationSubStation { get; set; }
        public User? ApprovedBy { get; set; }
        public Oil? Oil { get; set; }
    }
}
