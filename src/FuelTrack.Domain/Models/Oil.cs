using System.ComponentModel.DataAnnotations.Schema;

namespace FuelTrack.Domain.Models
{
    [Table("Oils")]
    public class Oil : BaseEntity
    {
        public int OilLookupId { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal MinStock { get; set; }
        public int? StationId { get; set; }
        public int? SubStationId { get; set; }

        public Lookup OilLookup { get; set; } = null!;
        public Station? Station { get; set; }
        public SubStation? SubStation { get; set; }
    }
}
