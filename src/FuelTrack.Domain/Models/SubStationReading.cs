using System.ComponentModel.DataAnnotations.Schema;

namespace FuelTrack.Domain.Models
{
    public class SubStationReading : BaseEntity
    {
        public int SubStationId { get; set; }
        public int? MeterId { get; set; }
        public DateTime ReadingDate { get; set; }

        public double PrevExportedEnergyKWh { get; set; }
        public double CurrentExportedEnergyKWh { get; set; }

        // Energy consumed internally at the site (optional).
        public double? InternalConsumption { get; set; }

        public string? Notes { get; set; }

        public int ReadingStatusLookupId { get; set; }
        public string? RejectionReason { get; set; }

        public SubStation SubStation { get; set; } = null!;
        public Meter? Meter { get; set; }
        public Lookup ReadingStatusLookup { get; set; } = null!;
    }
}
