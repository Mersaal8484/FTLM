using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Models;

/// <summary>
/// Service interval definition per generator model and service item.
/// Independent of the parts table (nullable SparePartId deferred to Phase 5).
/// </summary>
public class ServiceIntervalDefinition : BaseEntity
{
    public string GeneratorModelKey { get; set; } = string.Empty;
    public int ServiceItemId { get; set; }
    public decimal IntervalHours { get; set; }
    public ServiceIntervalImportance Importance { get; set; } = ServiceIntervalImportance.Normal;
    public decimal? WarningLeadHours { get; set; }
    public string? Notes { get; set; }

    public ServiceItem ServiceItem { get; set; } = null!;
}
