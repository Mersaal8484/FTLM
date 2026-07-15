using System.ComponentModel.DataAnnotations.Schema;

namespace FuelTrack.Domain.Models
{
    public class DailyReading : BaseEntity
    {
            public int GeneratorId { get; set; }
        public int? ControllerId { get; set; }
        public DateTime ReadingDate { get; set; }

        public double PrevEnergyKWh { get; set; }
        public double CurrentEnergyKWh { get; set; }

        public double PrevRunningHours { get; set; }
        public double CurrentRunningHours { get; set; }

        public int FuelLookupId { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal StandardFuelRate { get; set; }

        public int OilLookupId { get; set; }
        public double OilUsedLiters { get; set; }

        public int ReadingStatusLookupId { get; set; }
        public string? RejectionReason { get; set; }
        public string? Notes { get; set; }

        public Generator Generator { get; set; } = null!;
        public Controller? Controller { get; set; }
        public Lookup FuelLookup { get; set; } = null!;
        public Lookup OilLookup { get; set; } = null!;
        public Lookup ReadingStatusLookup { get; set; } = null!;
    }
}
