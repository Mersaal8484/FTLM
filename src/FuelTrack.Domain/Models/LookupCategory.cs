namespace FuelTrack.Domain.Models
{
    public class LookupCategory : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ICollection<Lookup> Items { get; set; } = new List<Lookup>();
    }
}
