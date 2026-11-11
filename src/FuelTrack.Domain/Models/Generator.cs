using System.ComponentModel.DataAnnotations.Schema;

namespace FuelTrack.Domain.Models
{
    public class Generator : BaseEntity
    {
        public int SubStationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        
        public string? Model { get; set; }
        public string? Manufacturer { get; set; }
        public string? OwnerName { get; set; }

        public double NominalPower { get; set; }
        public int? NominalPowerUnitLookupId { get; set; }
        public int ActualPowerKW { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal FuelConsumptionRatePerHour { get; set; }

        public int? GeneratorTypeLookupId { get; set; }
        public int? VoltageLookupId { get; set; }
        public int? FrequencyLookupId { get; set; }
        public double? PowerFactor { get; set; }
        public int? OwnershipTypeLookupId { get; set; }
        public int? StatusLookupId { get; set; }
        
        public DateTime? InstallationDate { get; set; }
        public string? Notes { get; set; }

        public SubStation SubStation { get; set; } = null!;
        public Controller? Controller { get; set; }
        public Lookup? NominalPowerUnitLookup { get; set; }
        public Lookup? GeneratorTypeLookup { get; set; }
        public Lookup? VoltageLookup { get; set; }
        public Lookup? FrequencyLookup { get; set; }
        public Lookup? OwnershipTypeLookup { get; set; }
        public Lookup? StatusLookup { get; set; }
        
        public ICollection<DailyReading> DailyReadings { get; set; } = new List<DailyReading>();
        public ICollection<ServiceReplacementRecord> ServiceReplacementRecords { get; set; } = new List<ServiceReplacementRecord>();
        public ICollection<ServiceDueState> ServiceDueStates { get; set; } = new List<ServiceDueState>();
    }
}
