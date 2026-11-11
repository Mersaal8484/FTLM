using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Models;

/// <summary>
/// Immutable record of a service replacement or baseline on a generator.
/// Voidable with reason, but never edited.
/// </summary>
public class ServiceReplacementRecord : BaseEntity
{
    public int GeneratorId { get; set; }
    public int ServiceItemId { get; set; }
    public ServiceRecordKind Kind { get; set; } = ServiceRecordKind.Replacement;
    public decimal HoursAtService { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public int? WorkPermitId { get; set; }
    public bool IsEarly { get; set; }
    public string? EarlyReplacementReason { get; set; }
    public decimal? IntervalHoursSnapshot { get; set; }
    public string? Notes { get; set; }

    public DateTime? VoidedAtUtc { get; set; }
    public int? VoidedByUserId { get; set; }
    public string? VoidReason { get; set; }

    public Generator Generator { get; set; } = null!;
    public ServiceItem ServiceItem { get; set; } = null!;
    public WorkPermit? WorkPermit { get; set; }
}
