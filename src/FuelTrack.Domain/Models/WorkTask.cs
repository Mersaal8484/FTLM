namespace FuelTrack.Domain.Models
{
    public class WorkTask : BaseEntity
    {
        public int WorkPermitId { get; set; }
        public string TaskDescription { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? CompletedById { get; set; }
        public string? Notes { get; set; }

        public WorkPermit WorkPermit { get; set; } = null!;
        public User? CompletedBy { get; set; }
    }
}
