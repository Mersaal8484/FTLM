namespace FuelTrack.Application.DTOs.Operations;

/// <summary>Read-only quality indicators for one generator business day.</summary>
public sealed record OperationalCompletenessDto(
    int GeneratorId,
    DateOnly BusinessDate,
    decimal? AverageLoadKw,
    decimal? MeanActiveLoadKw,
    bool AverageLoadWarning,
    IReadOnlyList<string> MissingSlots,
    bool MissingDailyClosing);

/// <summary>Read-only quality indicators for one feeder business day.</summary>
public sealed record FeederOperationalCompletenessDto(
    int FeederId,
    DateOnly BusinessDate,
    IReadOnlyList<string> MissingSlots,
    bool MissingDailyClosing);
