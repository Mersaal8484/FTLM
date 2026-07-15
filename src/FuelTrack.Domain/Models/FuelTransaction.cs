using System.ComponentModel.DataAnnotations.Schema;

namespace FuelTrack.Domain.Models
{
    public class FuelTransaction : BaseEntity
    {
        public int SubStationId { get; set; }
        public int FuelLookupId { get; set; }
        public int? TransactionTypeLookupId { get; set; }
        public DateTime TransactionDate { get; set; }
        public double QuantityLiters { get; set; }
        public string? Reference { get; set; }

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
        
        // قراءات عداد سحب الوقود في الموقع عند الاستلام
        public int? MeterId { get; set; }
        public double? PrevMeterVisualReading { get; set; }
        public double? CurrentMeterVisualReading { get; set; }
        public double? PrevMeterStandardReading { get; set; }
        public double? CurrentMeterStandardReading { get; set; }
        
        // -- النقل (صادر / وارد بين المواقع) --
        public int? SourceSubStationId { get; set; }
        public int? DestinationSubStationId { get; set; }

        // التصحيح
        public int? CorrectedFromId { get; set; }
        public string? CorrectionReason { get; set; }

        // الاعتماد المزدوج
        public bool IsApproved { get; set; } = false;        
        public int? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        public string? Notes { get; set; }
        
        public SubStation SubStation { get; set; } = null!;
        public Lookup FuelLookup { get; set; } = null!;
        public Lookup TransactionTypeLookup { get; set; } = null!;
        public Meter? Meter { get; set; }
        public SubStation? SourceSubStation { get; set; }
        public SubStation? DestinationSubStation { get; set; }
        public User? ApprovedBy { get; set; }
    }
}
