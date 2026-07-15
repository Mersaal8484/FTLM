namespace FuelTrack.Domain.Models
{
    public class Branch : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsGeneration { get; set; }
        public string? Notes { get; set; }

        public ICollection<Station> Stations { get; set; } = new List<Station>();
    }
}
