
namespace FuelTrack.Domain.Models
{
    public class InsightFeedback : BaseEntity
    {
        public int AnalyticsInsightId { get; set; }
        public int UserId { get; set; }
        public bool IsHelpful { get; set; }
        public string? Comment { get; set; }
        public DateTime FeedbackAt { get; set; }

        public AnalyticsInsight AnalyticsInsight { get; set; } = null!;
    }
}
