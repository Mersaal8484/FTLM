using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FuelTrack.Domain.Models
{
    public class WarehouseStock : BaseEntity
    {
        public int SparePartId { get; set; }
        public int StationId { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal CurrentQuantity { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal ReservedQuantity { get; set; }

        [NotMapped]
        public decimal AvailableQuantity => CurrentQuantity - ReservedQuantity;

        public SparePart SparePart { get; set; } = null!;
        public Station Station { get; set; } = null!;
    }
}
