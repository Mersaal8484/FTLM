using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Models;

/// <summary>Optional station-configured operating band. Null bounds are intentionally allowed.</summary>
public sealed class GeneratorOperatingLimit : BaseEntity
{
    public int GeneratorId { get; set; }
    public GeneratorOperatingParameter Parameter { get; set; }
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
    public int? LastEvaluatedReadingId { get; set; }
    public DateTime? LastEvaluatedCapturedAtUtc { get; set; }
    public Generator Generator { get; set; } = null!;
}
