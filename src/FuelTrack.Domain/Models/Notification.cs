
namespace FuelTrack.Domain.Models
{
    public class Notification : BaseEntity
    {
        public int? SubStationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public string? Severity { get; set; }
        public bool IsRead { get; set; }
        public DateTime? SentAt { get; set; }
        public int? SentById { get; set; }

        public SubStation? SubStation { get; set; }
        public ICollection<NotificationRecipient> Recipients { get; set; } = new List<NotificationRecipient>();
    }
}
