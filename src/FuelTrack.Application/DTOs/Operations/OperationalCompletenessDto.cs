namespace FuelTrack.Application.DTOs.Operations;

/// <summary>Read-only quality indicators for one generator business day.</summary>
public sealed record OperationalCompletenessDto(
    int GeneratorId,
    DateOnly BusinessDate,
    decimal? AverageLoadKw,
    decimal? MeanActiveLoadKw,
    bool AverageLoadWarning,
    IReadOnlyList<int> MissingHours,
    bool MissingDailyClosing);
