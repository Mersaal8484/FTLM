
namespace FuelTrack.Domain.Models
{
    public class PeriodicInventory : BaseEntity
    {
        public int SubStationId { get; set; }
        
        // فترة الجرد
        public int Year { get; set; }
        public int Month { get; set; }
        public int PeriodTypeLookupId { get; set; } // النصف الأول / النصف الثاني
        
        public DateTime InventoryDate { get; set; } // تاريخ الإدخال الفعلي للجرد
        public string? Notes { get; set; }
        
        public bool IsApproved { get; set; } = false;
        public int? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public SubStation SubStation { get; set; } = null!;
        public Lookup PeriodTypeLookup { get; set; } = null!;
        public User? ApprovedBy { get; set; }

        public ICollection<PeriodicInventoryFuel> FuelReadings { get; set; } = new List<PeriodicInventoryFuel>();
        public ICollection<PeriodicInventoryOil> OilReadings { get; set; } = new List<PeriodicInventoryOil>();
    }
}

