using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

/// <summary>
/// Phase 3: daily delta computation and monotonicity enforcement for meter readings.
/// delta = (closing_today.RawValue − prev.RawValue) × closing_today.MultiplierSnapshot
/// </summary>
public sealed class MeterDeltaService(FuelTrackDbContext db) : IMeterDeltaService
{
    public async Task<MeterDeltaDto?> GetIntervalDeltaAsync(int meterReadingId, CancellationToken cancellationToken = default)
    {
        var current = await db.MeterReadings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == meterReadingId && x.Status != OperationalReadingStatus.Voided && x.Status != OperationalReadingStatus.Rejected, cancellationToken);
        if (current is null) return null;
        if (current.IsMeterReset)
            return new MeterDeltaDto(current.BusinessDate, null, null, current.RawValue, null, true);

        var previous = await db.MeterReadings.AsNoTracking()
            .Where(x => x.SubjectType == current.SubjectType && x.SubjectId == current.SubjectId &&
                        x.Status != OperationalReadingStatus.Voided && x.Status != OperationalReadingStatus.Rejected &&
                        (x.CapturedAtUtc < current.CapturedAtUtc || (x.CapturedAtUtc == current.CapturedAtUtc && x.Id < current.Id)))
            .OrderByDescending(x => x.CapturedAtUtc).ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (previous is null)
            return new MeterDeltaDto(current.BusinessDate, null, null, current.RawValue, null, false);

        var span = current.BusinessDate.DayNumber - previous.BusinessDate.DayNumber;
        return new MeterDeltaDto(
            current.BusinessDate,
            (current.RawValue - previous.RawValue) * current.MultiplierSnapshot,
            previous.RawValue,
            current.RawValue,
            span > 1 ? span : null,
            false);
    }

    public async Task<MeterDeltaDto?> GetDailyDeltaAsync(
        MeterReadingSubjectType subjectType,
        int subjectId,
        DateOnly businessDate,
        CancellationToken cancellationToken = default)
    {
        // Find the approved DailyClosing for this subject on this date
        var today = await db.MeterReadings
            .AsNoTracking()
            .Where(r => r.SubjectType == subjectType
                        && r.SubjectId == subjectId
                        && r.Kind == MeterReadingKind.DailyClosing
                        && r.BusinessDate == businessDate
                        && ReadingApprovalPolicy.IsOfficial(r.Status))
            .OrderByDescending(r => r.CapturedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (today is null) return null;

        if (today.IsMeterReset)
            return new MeterDeltaDto(businessDate, null, null, today.RawValue, null, true);

        // Find the previous approved DailyClosing
        var prev = await db.MeterReadings
            .AsNoTracking()
            .Where(r => r.SubjectType == subjectType
                        && r.SubjectId == subjectId
                        && r.Kind == MeterReadingKind.DailyClosing
                        && r.CapturedAtUtc < today.CapturedAtUtc
                        && ReadingApprovalPolicy.IsOfficial(r.Status))
            .OrderByDescending(r => r.CapturedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (prev is null)
            return new MeterDeltaDto(businessDate, null, null, today.RawValue, null, false);

        var delta = (today.RawValue - prev.RawValue) * today.MultiplierSnapshot;
        int spanDays = businessDate.DayNumber - prev.BusinessDate.DayNumber;

        return new MeterDeltaDto(businessDate, delta, prev.RawValue, today.RawValue,
            spanDays > 1 ? spanDays : null, false);
    }

    public async Task<Result> ValidateMonotonicityAsync(
        int meterReadingId, CancellationToken cancellationToken = default)
    {
        var reading = await db.MeterReadings
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == meterReadingId, cancellationToken);

        if (reading is null)
            return Result.NotFound("Meter reading not found.");

        if (reading.IsMeterReset)
            return Result.Success(); // reset always bypasses monotonicity

        // Previous approved reading for same subject
        var prev = await db.MeterReadings
            .AsNoTracking()
            .Where(r => r.SubjectType == reading.SubjectType
                        && r.SubjectId == reading.SubjectId
                        && r.CapturedAtUtc < reading.CapturedAtUtc
                        && ReadingApprovalPolicy.IsOfficial(r.Status))
            .OrderByDescending(r => r.CapturedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (prev is not null && prev.RawValue > reading.RawValue)
            return Result.Failure(
                $"Reading value {reading.RawValue} is less than the previous approved value {prev.RawValue}. " +
                "Flag IsMeterReset to override.", 422);

        // Next approved reading for same subject
        var next = await db.MeterReadings
            .AsNoTracking()
            .Where(r => r.SubjectType == reading.SubjectType
                        && r.SubjectId == reading.SubjectId
                        && r.CapturedAtUtc > reading.CapturedAtUtc
                        && ReadingApprovalPolicy.IsOfficial(r.Status))
            .OrderBy(r => r.CapturedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (next is not null && !next.IsMeterReset && reading.RawValue > next.RawValue)
            return Result.Failure(
                $"Reading value {reading.RawValue} is greater than the next approved value {next.RawValue}. " +
                "Approval would break monotonic order.", 422);

        return Result.Success();
    }
}
