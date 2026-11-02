using System.ComponentModel.DataAnnotations.Schema;

namespace FuelTrack.Domain.Models
{
    public class WarehouseSupplyItem : BaseEntity
    {
        public int WarehouseSupplyId { get; set; }
        public int SparePartId { get; set; }
        public decimal Quantity { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitPrice { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalPrice { get; set; }
        public string? SerialNumber { get; set; }
        public string? Notes { get; set; }

        public WarehouseSupply WarehouseSupply { get; set; } = null!;
        public SparePart SparePart { get; set; } = null!;
    }
}
