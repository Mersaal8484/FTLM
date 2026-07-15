
namespace FuelTrack.Domain.Models
{
    public class AnalyticsInsight : BaseEntity
    {
        public int? SubStationId { get; set; }
        public int? StationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string InsightType { get; set; } = string.Empty;
        public string? Severity { get; set; }
        public string? Category { get; set; }
        public string? DataPayload { get; set; }
        public DateTime GeneratedAt { get; set; }

        public ICollection<InsightFeedback> Feedback { get; set; } = new List<InsightFeedback>();
    }
}
