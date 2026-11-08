
namespace FuelTrack.Domain.Models
{
    public class SubStation : BaseEntity
    {
        public int StationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Location { get; set; }
        public double? CapacityKW { get; set; }

        public Station Station { get; set; } = null!;
        public ICollection<Generator> Generators { get; set; } = new List<Generator>();
        public ICollection<FuelTank> FuelTanks { get; set; } = new List<FuelTank>();
        public ICollection<Meter> Meters { get; set; } = new List<Meter>();
        public ICollection<Feeder> Feeders { get; set; } = new List<Feeder>();
        public ICollection<OperationalSubmission> OperationalSubmissions { get; set; } = new List<OperationalSubmission>();
    }
}
