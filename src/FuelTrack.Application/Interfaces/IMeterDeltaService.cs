using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Common;
using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.Interfaces;

public interface IMeterDeltaService
{
    /// <summary>
    /// Returns the daily delta for an approved DailyClosing reading.
    /// delta = (today - prev) * multiplierSnapshot.
    /// Returns null if no approved closing exists for <paramref name="businessDate"/>.
    /// </summary>
    Task<MeterDeltaDto?> GetDailyDeltaAsync(
        MeterReadingSubjectType subjectType,
        int subjectId,
        DateOnly businessDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Display-only delta between this raw reading and the immediately preceding usable reading
    /// for the same counter. This never contributes to official balances.
    /// </summary>
    Task<MeterDeltaDto?> GetIntervalDeltaAsync(
        int meterReadingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Order-independence check at approval time (§7.5).
    /// Returns Failure if approving <paramref name="meterReadingId"/> would break monotonic order
    /// (unless IsMeterReset is true on the affected line).
    /// </summary>
    Task<Result> ValidateMonotonicityAsync(
        int meterReadingId,
        CancellationToken cancellationToken = default);
}
