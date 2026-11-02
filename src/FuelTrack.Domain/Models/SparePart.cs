using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FuelTrack.Domain.Models
{
    public class SparePart : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string PartNumber { get; set; } = string.Empty;
        public string? ManufacturerPartNumber { get; set; }
        public string? Barcode { get; set; }
        public int PartCategoryLookupId { get; set; }
        public int UnitLookupId { get; set; }
        public string? Description { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitPrice { get; set; }
        public int MinStockQuantity { get; set; }
        public int? ReplacementIntervalHours { get; set; }
        public int? ReplacementPriorityLookupId { get; set; }
        public bool IsHoursTracked { get; set; }
        public string? Notes { get; set; }

        public Lookup PartCategoryLookup { get; set; } = null!;
        public Lookup UnitLookup { get; set; } = null!;
        public Lookup? ReplacementPriorityLookup { get; set; }
        public ICollection<WarehouseStock> Stocks { get; set; } = new List<WarehouseStock>();
        public ICollection<WarehouseSupplyItem> SupplyItems { get; set; } = new List<WarehouseSupplyItem>();
        public ICollection<DispatchRequestItem> DispatchItems { get; set; } = new List<DispatchRequestItem>();
        public ICollection<InstalledPart> InstalledParts { get; set; } = new List<InstalledPart>();
    }
}
