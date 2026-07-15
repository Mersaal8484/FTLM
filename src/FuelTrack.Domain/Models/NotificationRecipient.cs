
namespace FuelTrack.Domain.Models
{
    public class NotificationRecipient : BaseEntity
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        public Notification Notification { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
