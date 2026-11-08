namespace FuelTrack.Domain.Models
{
    public class Station : BaseEntity
    {
        public int BranchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Notes { get; set; }

        public Branch Branch { get; set; } = null!;
        public ICollection<SubStation> SubStations { get; set; } = new List<SubStation>();
        public StationOperationalSettings? OperationalSettings { get; set; }
    }
}
