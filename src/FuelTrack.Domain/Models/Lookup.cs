namespace FuelTrack.Domain.Models
{
    public class Lookup : BaseEntity
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public int SortOrder { get; set; } = 0;

        public LookupCategory Category { get; set; } = null!;
    }
}