namespace FuelTrack.Domain.Models;

/// <summary>
/// Per-customer capability switch. Each tenant has its own operational database,
/// so no external customer identifier is persisted here.
/// </summary>
public sealed class CustomerFeatureSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? Value { get; set; }
}
