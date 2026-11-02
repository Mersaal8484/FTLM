namespace FuelTrack.Domain.Models
{
    public class WorkPermit : BaseEntity
    {
        public string PermitNumber { get; set; } = string.Empty;
        public int SubStationId { get; set; }
        public int? GeneratorId { get; set; }
        public int MaintenanceTypeLookupId { get; set; }
        public int DepartmentLookupId { get; set; }
        public int PriorityLookupId { get; set; }
        public int StatusLookupId { get; set; }
        public DateTime PermitDate { get; set; }
        public string ProblemDescription { get; set; } = string.Empty;
        public string? ProblemCause { get; set; }
        public DateTime? PlannedStartDate { get; set; }
        public DateTime? PlannedEndDate { get; set; }
        public DateTime? ActualStartDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public double? RunningHoursAtCreation { get; set; }
        public int RequestedById { get; set; }
        public int? AssignedToId { get; set; }
        public int? ApprovedById { get; set; }
        public string? CompletionNotes { get; set; }
        public string? RejectionReason { get; set; }
        public string? Notes { get; set; }

        public SubStation SubStation { get; set; } = null!;
        public Generator? Generator { get; set; }
        public Lookup MaintenanceTypeLookup { get; set; } = null!;
        public Lookup DepartmentLookup { get; set; } = null!;
        public Lookup PriorityLookup { get; set; } = null!;
        public Lookup StatusLookup { get; set; } = null!;
        public User RequestedBy { get; set; } = null!;
        public User? AssignedTo { get; set; }
        public User? ApprovedBy { get; set; }
        public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
        public ICollection<DispatchRequest> DispatchRequests { get; set; } = new List<DispatchRequest>();
        public ICollection<InstalledPart> InstalledParts { get; set; } = new List<InstalledPart>();
        public ICollection<MaintenanceLog> Logs { get; set; } = new List<MaintenanceLog>();
    }
}
