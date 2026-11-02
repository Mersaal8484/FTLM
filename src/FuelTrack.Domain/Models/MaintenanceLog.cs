namespace FuelTrack.Domain.Models
{
    public class MaintenanceLog : BaseEntity
    {
        public int WorkPermitId { get; set; }
        public int? GeneratorId { get; set; }
        public DateTime LogDate { get; set; }
        public double? RunningHoursAtLog { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int LoggedById { get; set; }

        public WorkPermit WorkPermit { get; set; } = null!;
        public Generator? Generator { get; set; }
        public User LoggedBy { get; set; } = null!;
    }
}
