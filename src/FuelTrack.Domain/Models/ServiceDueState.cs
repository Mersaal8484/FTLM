using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Models;

/// <summary>
/// Persistent state tracking for service-due evaluation and alert deduplication.
/// Remembers the current cycle and highest alert level dispatched to prevent duplicates.
/// </summary>
public class ServiceDueState : BaseEntity
{
    public int GeneratorId { get; set; }
    public int ServiceItemId { get; set; }
    public int? CycleRecordId { get; set; }
    public ServiceDueStatus? HighestNotifiedStatus { get; set; }
    public DateTime? LastEvaluatedAtUtc { get; set; }

    public Generator Generator { get; set; } = null!;
    public ServiceItem ServiceItem { get; set; } = null!;
    public ServiceReplacementRecord? CycleRecord { get; set; }
}
