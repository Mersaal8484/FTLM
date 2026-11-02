namespace FuelTrack.Domain.Models
{
    public class InstalledPart : BaseEntity
    {
        public int GeneratorId { get; set; }
        public int SparePartId { get; set; }
        public int? WorkPermitId { get; set; }
        public int? DispatchRequestItemId { get; set; }
        public DateTime InstallationDate { get; set; }
        public double InstallationHours { get; set; }
        public int? ReplacementIntervalHours { get; set; }
        public double? EstimatedReplacementAtHours { get; set; }
        public bool IsActive { get; set; }
        public DateTime? RemovedAt { get; set; }
        public double? RemovedAtHours { get; set; }
        public string? EarlyReplacementReason { get; set; }
        public string? Notes { get; set; }

        public Generator Generator { get; set; } = null!;
        public SparePart SparePart { get; set; } = null!;
        public WorkPermit? WorkPermit { get; set; }
        public DispatchRequestItem? DispatchRequestItem { get; set; }
    }
}
