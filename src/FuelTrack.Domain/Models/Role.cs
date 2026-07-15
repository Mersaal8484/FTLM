using Microsoft.AspNetCore.Identity;

namespace FuelTrack.Domain.Models
{
    public class Role : IdentityRole<int>
    {
        public string? Key { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
