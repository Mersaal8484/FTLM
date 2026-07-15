namespace FuelTrack.Domain.Models
{
    public class Meter : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        public int? StationId { get; set; }
        public int? SubStationId { get; set; }
        public string? MeterType { get; set; }
        public int? MeterTypeLookupId { get; set; }
        public string? SerialNumber { get; set; }

        public int? GeneratorId { get; set; }
        public int? FuelTankId { get; set; }

        public double? HoursReading { get; set; }
        public double? EnergyReading { get; set; }

        public Station? Station { get; set; }
        public SubStation? SubStation { get; set; }
        public Lookup? MeterTypeLookup { get; set; }
        public Generator? Generator { get; set; }
        public FuelTank? FuelTank { get; set; }
    }
}
