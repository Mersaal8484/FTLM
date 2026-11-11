namespace FuelTrack.Domain.Models;

/// <summary>
/// Hour-tracked maintenance service item (e.g. Oil Change Set, Air Filter).
/// Independent table from legacy lookup and spare parts.
/// </summary>
public class ServiceItem : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public decimal? SuggestedIntervalHours { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ServiceIntervalDefinition> Definitions { get; set; } = new List<ServiceIntervalDefinition>();
    public ICollection<ServiceReplacementRecord> ReplacementRecords { get; set; } = new List<ServiceReplacementRecord>();
    public ICollection<ServiceDueState> DueStates { get; set; } = new List<ServiceDueState>();
}
