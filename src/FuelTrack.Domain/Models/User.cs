using Microsoft.AspNetCore.Identity;

namespace FuelTrack.Domain.Models
{
    public class User : IdentityUser<int>
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool TwoFactorEnabled { get; set; } = false;
        public string? TwoFactorSecret { get; set; }
        public string? TwoFactorBackupCodes { get; set; }
        public ICollection<UserScope> Scopes { get; set; } = new List<UserScope>();
        public ICollection<NotificationRecipient> Notifications { get; set; } = new List<NotificationRecipient>();
    }
}
