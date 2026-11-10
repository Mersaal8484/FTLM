using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Notifications;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FuelTrack.Infrastructure.Services;

/// <summary>
/// Phase 3: Tier-2 review queue, approve/reject workflow.
/// Only lines with Status=Pending appear in the queue.
/// Tier-1 (Recorded) lines are never touched here.
/// </summary>
public sealed class OperationalReviewService(
    FuelTrackDbContext db,
    IUserScopeFilterService scopeFilter,
    INotificationService notifications,
    IMeterDeltaService meterDelta,
    IFuelLoadSafetyService fuelLoadSafety,
    ILogger<OperationalReviewService>? logger = null) : IOperationalReviewService
{
    private static readonly TimeSpan SkewThreshold = TimeSpan.FromHours(12);
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> SubjectLocks = new();

    // ── Queue ────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<PendingSubmissionDto>> GetPendingSubmissionsAsync(
        int userId, int? subStationId, CancellationToken cancellationToken = default)
    {
        var accessibleSiteIds = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);

        var query = db.OperationalSubmissions
            .AsNoTracking()
            .Where(s => s.Status == OperationalReadingStatus.Pending
                        && accessibleSiteIds.Contains(s.SubStationId));

        if (subStationId.HasValue)
            query = query.Where(s => s.SubStationId == subStationId.Value);

        // Load reading timestamps client-side to avoid SQL Server-specific EF.Functions.DateDiffHour
        var rows = await query
            .OrderByDescending(s => s.ReceivedAtUtc)
            .Select(s => new
            {
                s.Id,
                s.SubStationId,
                SiteName = s.SubStation.Name,
                s.ReceivedAtUtc,
                s.SubmittedByUserId,
                SubmitterName = db.Users
                    .Where(u => u.Id == (s.SubmittedByUserId ?? s.CreatedBy))
                    .Select(u => u.FullName)
                    .FirstOrDefault() ?? string.Empty,
                GenCount = s.GeneratorReadings.Count(r => r.DeletedAt == null),
                TankCount = s.TankLevelReadings.Count(r => r.DeletedAt == null),
                FeedCount = s.FeederLoadReadings.Count(r => r.DeletedAt == null),
                MeterCount = s.MeterReadings.Count(r => r.DeletedAt == null),
                // Retrieve timestamps for skew computation on the client side
                GenTimestamps = s.GeneratorReadings
                    .Where(r => r.DeletedAt == null)
                    .Select(r => new { r.CapturedAtUtc, r.ReceivedAtUtc }),
                MeterTimestamps = s.MeterReadings
                    .Where(r => r.DeletedAt == null)
                    .Select(r => new { r.CapturedAtUtc, r.ReceivedAtUtc }),
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r =>
        {
            // Compute skew client-side — avoids SQL Server-specific EF functions
            var hasSkew = r.GenTimestamps.Any(t => (t.ReceivedAtUtc - t.CapturedAtUtc).Duration() > SkewThreshold)
                       || r.MeterTimestamps.Any(t => (t.ReceivedAtUtc - t.CapturedAtUtc).Duration() > SkewThreshold);

            return new PendingSubmissionDto(
                r.Id, r.SubStationId, r.SiteName, r.ReceivedAtUtc,
                r.SubmittedByUserId ?? 0, r.SubmitterName,
                r.GenCount, r.TankCount, r.FeedCount, r.MeterCount,
                hasSkew);
        }).ToList();
    }

    // ── Detail ───────────────────────────────────────────────────────────────

    public async Task<Result<SubmissionDetailDto>> GetSubmissionDetailAsync(
        int submissionId, int userId, CancellationToken cancellationToken = default)
    {
        var accessibleSiteIds = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);

        var submission = await db.OperationalSubmissions
            .AsNoTracking()
            .Where(s => s.Id == submissionId && accessibleSiteIds.Contains(s.SubStationId))
            .Select(s => new
            {
                s.Id, s.SubStationId,
                SiteName = s.SubStation.Name,
                s.ReceivedAtUtc, s.SubmittedByUserId, s.Status,
                SubmitterName = db.Users
                    .Where(u => u.Id == (s.SubmittedByUserId ?? s.CreatedBy))
                    .Select(u => u.FullName)
                    .FirstOrDefault() ?? string.Empty,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (submission is null)
            return Result<SubmissionDetailDto>.NotFound("Submission not found or outside your scope.");

        // Collect all photo attachment IDs linked to each line for reuse-warning detection
        var allSubmissionPhotoHashes = await GetSubmissionPhotoHashesAsync(submissionId, cancellationToken);

        // Generator lines
        var genLines = await db.OperationalGeneratorReadings
            .AsNoTracking()
            .Where(r => r.OperationalSubmissionId == submissionId)
            .Select(r => new
            {
                r.Id, r.Status, r.CapturedAtUtc, r.ReceivedAtUtc, r.WarningsJson,
                r.RejectionReason, r.IsRunning,
                r.ActiveLoadKw, r.ApparentLoadKva, r.FrequencyHz, r.PowerFactor,
                r.OilPressure, r.WaterTemperatureCelsius, r.PeakLoadKw,
            })
            .ToListAsync(cancellationToken);

        var genPhotoIds = await GetLinePhotoIdsAsync("OperationalGeneratorReading",
            genLines.Select(g => g.Id).ToList(), cancellationToken);

        var genReuseWarnings = await GetReuseWarningSetAsync(genPhotoIds, submissionId, cancellationToken);

        var genDtos = genLines.Select(r => new ReviewLineDto(
            r.Id, "generator", r.Status.ToString(),
            r.CapturedAtUtc, r.ReceivedAtUtc,
            (r.ReceivedAtUtc - r.CapturedAtUtc).Duration() > SkewThreshold,
            genPhotoIds.GetValueOrDefault(r.Id, []),
            r.WarningsJson,
            r.RejectionReason,
            genPhotoIds.GetValueOrDefault(r.Id, []).Any(pid => genReuseWarnings.Contains(pid)),
            new { r.IsRunning, r.ActiveLoadKw, r.ApparentLoadKva, r.FrequencyHz, r.PowerFactor, r.OilPressure, r.WaterTemperatureCelsius, r.PeakLoadKw }
        )).ToList();

        // Tank lines
        var tankLines = await db.TankLevelReadings
            .AsNoTracking()
            .Where(r => r.OperationalSubmissionId == submissionId)
            .Select(r => new
            {
                r.Id, r.Status, r.CapturedAtUtc, r.ReceivedAtUtc,
                r.LiquidLevelCm, r.ComputedLitres, r.EventType,
                r.BeforeLiquidLevelCm, r.DeliveryNoteLitres,
                r.WarningsJson, r.RejectionReason,
            })
            .ToListAsync(cancellationToken);

        var tankPhotoIds = await GetLinePhotoIdsAsync("TankLevelReading",
            tankLines.Select(t => t.Id).ToList(), cancellationToken);
        var tankReuseWarnings = await GetReuseWarningSetAsync(tankPhotoIds, submissionId, cancellationToken);

        var tankDtos = tankLines.Select(r => new ReviewLineDto(
            r.Id, "tank", r.Status.ToString(),
            r.CapturedAtUtc, r.ReceivedAtUtc,
            (r.ReceivedAtUtc - r.CapturedAtUtc).Duration() > SkewThreshold,
            tankPhotoIds.GetValueOrDefault(r.Id, []),
            r.WarningsJson, r.RejectionReason,
            tankPhotoIds.GetValueOrDefault(r.Id, []).Any(pid => tankReuseWarnings.Contains(pid)),
            new { r.LiquidLevelCm, r.ComputedLitres, r.EventType, r.BeforeLiquidLevelCm, r.DeliveryNoteLitres }
        )).ToList();

        // Feeder lines
        var feederLines = await db.FeederLoadReadings
            .AsNoTracking()
            .Where(r => r.OperationalSubmissionId == submissionId)
            .Select(r => new
            {
                r.Id, r.Status, r.CapturedAtUtc, r.ReceivedAtUtc,
                r.ActiveLoadKw, r.ApparentLoadKva,
                r.WarningsJson, r.RejectionReason,
            })
            .ToListAsync(cancellationToken);

        var feederPhotoIds = await GetLinePhotoIdsAsync("FeederLoadReading",
            feederLines.Select(f => f.Id).ToList(), cancellationToken);
        var feederReuseWarnings = await GetReuseWarningSetAsync(feederPhotoIds, submissionId, cancellationToken);

        var feederDtos = feederLines.Select(r => new ReviewLineDto(
            r.Id, "feeder", r.Status.ToString(),
            r.CapturedAtUtc, r.ReceivedAtUtc,
            (r.ReceivedAtUtc - r.CapturedAtUtc).Duration() > SkewThreshold,
            feederPhotoIds.GetValueOrDefault(r.Id, []),
            r.WarningsJson, r.RejectionReason,
            feederPhotoIds.GetValueOrDefault(r.Id, []).Any(pid => feederReuseWarnings.Contains(pid)),
            new { r.ActiveLoadKw, r.ApparentLoadKva }
        )).ToList();

        // Meter lines with delta preview
        var meterLines = await db.MeterReadings
            .AsNoTracking()
            .Where(r => r.OperationalSubmissionId == submissionId)
            .ToListAsync(cancellationToken);

        var meterPhotoIds = await GetLinePhotoIdsAsync("MeterReading",
            meterLines.Select(m => m.Id).ToList(), cancellationToken);
        var meterReuseWarnings = await GetReuseWarningSetAsync(meterPhotoIds, submissionId, cancellationToken);

        var meterDtos = new List<ReviewMeterLineDto>();
        foreach (var r in meterLines)
        {
            MeterDeltaDto? delta = null;
            if (r.Kind == MeterReadingKind.DailyClosing)
                delta = await meterDelta.GetDailyDeltaAsync(r.SubjectType, r.SubjectId, r.BusinessDate, cancellationToken);

            meterDtos.Add(new ReviewMeterLineDto(
                r.Id, r.Status.ToString(), r.SubjectType, r.SubjectId, r.Kind,
                r.CapturedAtUtc, r.ReceivedAtUtc, r.BusinessDate,
                (r.ReceivedAtUtc - r.CapturedAtUtc).Duration() > SkewThreshold,
                meterPhotoIds.GetValueOrDefault(r.Id, []),
                r.RawValue, r.MultiplierSnapshot, r.IsMeterReset, r.ResetNote,
                r.WarningsJson, r.RejectionReason,
                delta?.PreviousValue,
                delta?.Delta,
                delta?.SpanDays));
        }

        return Result<SubmissionDetailDto>.Success(new SubmissionDetailDto(
            submission.Id, submission.SubStationId, submission.SiteName,
            submission.ReceivedAtUtc,
            submission.SubmittedByUserId ?? 0, submission.SubmitterName,
            submission.Status.ToString(),
            genDtos, tankDtos, feederDtos, meterDtos));
    }

    // ── Approve ──────────────────────────────────────────────────────────────

    public async Task<Result> ApproveSubmissionAsync(
        int submissionId, int userId, CancellationToken cancellationToken = default)
    {
        var accessibleSiteIds = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var submission = await db.OperationalSubmissions
            .Where(s => s.Id == submissionId && accessibleSiteIds.Contains(s.SubStationId))
            .SingleOrDefaultAsync(cancellationToken);

        if (submission is null)
            return Result.NotFound("Submission not found or outside your scope.");

        // Idempotent: already fully approved
        if (submission.Status == OperationalReadingStatus.Approved)
            return Result.Success();

        // Approver ≠ submitter (D18, configurable)
        var settings = await db.StationOperationalSettings
            .AsNoTracking()
            .Where(s => s.StationId == db.SubStations
                .Where(site => site.Id == submission.SubStationId)
                .Select(site => site.StationId)
                .FirstOrDefault())
            .Select(s => new { s.RequiresDifferentReviewer })
            .FirstOrDefaultAsync(cancellationToken);

        bool requiresDifferent = settings?.RequiresDifferentReviewer ?? true;
        int submittedBy = submission.SubmittedByUserId ?? submission.CreatedBy;
        if (requiresDifferent && submittedBy == userId)
            return Result.Failure("The approver must be different from the submitter (D18).", 403);

        var now = DateTime.UtcNow;

        // Approve all non-rejected Pending lines
        var genLines = await db.OperationalGeneratorReadings
            .Where(r => r.OperationalSubmissionId == submissionId
                        && r.Status == OperationalReadingStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var r in genLines)
        {
            r.Status = OperationalReadingStatus.Approved;
            r.ApprovedAt = now;
            r.ApprovedByUserId = userId;
        }

        var tankLines = await db.TankLevelReadings
            .Where(r => r.OperationalSubmissionId == submissionId
                        && r.Status == OperationalReadingStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var r in tankLines)
        {
            r.Status = OperationalReadingStatus.Approved;
            r.ApprovedAt = now;
            r.ApprovedByUserId = userId;
        }

        var feederLines = await db.FeederLoadReadings
            .Where(r => r.OperationalSubmissionId == submissionId
                        && r.Status == OperationalReadingStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var r in feederLines)
        {
            r.Status = OperationalReadingStatus.Approved;
            r.ApprovedAt = now;
            r.ApprovedByUserId = userId;
        }

        var meterLines = await db.MeterReadings
            .Where(r => r.OperationalSubmissionId == submissionId
                        && r.Status == OperationalReadingStatus.Pending)
            .ToListAsync(cancellationToken);

        // Monotonicity check for each DailyClosing meter line before bulk-approve
        foreach (var r in meterLines.Where(r => r.Kind == MeterReadingKind.DailyClosing))
        {
            var sem = SubjectLocks.GetOrAdd($"meter:{r.SubjectType}:{r.SubjectId}", _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync(cancellationToken);
            Result monoResult;
            try
            {
                monoResult = await meterDelta.ValidateMonotonicityAsync(r.Id, cancellationToken);
            }
            finally
            {
                sem.Release();
            }
            if (!monoResult.IsSuccess)
            {
                await tx.RollbackAsync(cancellationToken);
                return Result.Failure(monoResult.Error!, 422);
            }
        }

        foreach (var r in meterLines)
        {
            r.Status = OperationalReadingStatus.Approved;
            r.ApprovedAt = now;
            r.ApprovedByUserId = userId;
        }

        submission.Status = OperationalReadingStatus.Approved;
        submission.UpdatedAt = now;
        submission.UpdatedBy = userId;

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        // Tier-2 readings become official only after this transaction commits.
        // Evaluate the dead-stock episode now; rejected/pending lines never alert.
        foreach (var tankLine in tankLines)
        {
            try { await fuelLoadSafety.EvaluateTankDeadStockAsync(tankLine.Id, cancellationToken); }
            catch (Exception ex)
            {
                // Approval is already durable; retain it and make the post-commit
                // safety failure observable for retry/operations investigation.
                logger?.LogError(ex, "Dead-stock evaluation failed after approving tank reading {TankReadingId}", tankLine.Id);
            }
        }

        // Notify submitter
        await NotifySubmitterAsync(submission, submittedBy, "approved", submissionId, cancellationToken);

        return Result.Success();
    }

    // ── Reject line ──────────────────────────────────────────────────────────

    public async Task<Result> RejectLineAsync(
        string lineType, int lineId, RejectLineDto dto, int userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return Result.Failure("A rejection reason is required.", 422);

        var accessibleSiteIds = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);
        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        int submissionId;
        int submittedBy;

        switch (lineType.ToLowerInvariant())
        {
            case "generator":
            {
                var line = await db.OperationalGeneratorReadings
                    .Include(r => r.OperationalSubmission)
                    .Where(r => r.Id == lineId
                                && accessibleSiteIds.Contains(r.OperationalSubmission.SubStationId))
                    .SingleOrDefaultAsync(cancellationToken);
                if (line is null) return Result.NotFound("Generator reading not found.");
                if (!ReadingApprovalPolicy.CanApprove(line.Status)) return Result.Success(); // idempotent
                var settings = await GetSettingsAsync(line.OperationalSubmission.SubStationId, cancellationToken);
                submittedBy = line.OperationalSubmission.SubmittedByUserId ?? line.OperationalSubmission.CreatedBy;
                if ((settings?.RequiresDifferentReviewer ?? true) && submittedBy == userId)
                    return Result.Failure("The reviewer must differ from the submitter (D18).", 403);
                line.Status = OperationalReadingStatus.Rejected;
                line.RejectedAt = now;
                line.RejectedByUserId = userId;
                line.RejectionReason = dto.Reason.Trim();
                submissionId = line.OperationalSubmissionId;
                break;
            }
            case "tank":
            {
                var line = await db.TankLevelReadings
                    .Include(r => r.OperationalSubmission)
                    .Where(r => r.Id == lineId
                                && accessibleSiteIds.Contains(r.OperationalSubmission.SubStationId))
                    .SingleOrDefaultAsync(cancellationToken);
                if (line is null) return Result.NotFound("Tank reading not found.");
                if (!ReadingApprovalPolicy.CanApprove(line.Status)) return Result.Success();
                var settings = await GetSettingsAsync(line.OperationalSubmission.SubStationId, cancellationToken);
                submittedBy = line.OperationalSubmission.SubmittedByUserId ?? line.OperationalSubmission.CreatedBy;
                if ((settings?.RequiresDifferentReviewer ?? true) && submittedBy == userId)
                    return Result.Failure("The reviewer must differ from the submitter (D18).", 403);
                line.Status = OperationalReadingStatus.Rejected;
                line.RejectedAt = now;
                line.RejectedByUserId = userId;
                line.RejectionReason = dto.Reason.Trim();
                submissionId = line.OperationalSubmissionId;
                break;
            }
            case "feeder":
            {
                var line = await db.FeederLoadReadings
                    .Include(r => r.OperationalSubmission)
                    .Where(r => r.Id == lineId
                                && accessibleSiteIds.Contains(r.OperationalSubmission.SubStationId))
                    .SingleOrDefaultAsync(cancellationToken);
                if (line is null) return Result.NotFound("Feeder reading not found.");
                if (!ReadingApprovalPolicy.CanApprove(line.Status)) return Result.Success();
                var settings = await GetSettingsAsync(line.OperationalSubmission.SubStationId, cancellationToken);
                submittedBy = line.OperationalSubmission.SubmittedByUserId ?? line.OperationalSubmission.CreatedBy;
                if ((settings?.RequiresDifferentReviewer ?? true) && submittedBy == userId)
                    return Result.Failure("The reviewer must differ from the submitter (D18).", 403);
                line.Status = OperationalReadingStatus.Rejected;
                line.RejectedAt = now;
                line.RejectedByUserId = userId;
                line.RejectionReason = dto.Reason.Trim();
                submissionId = line.OperationalSubmissionId;
                break;
            }
            case "meter":
            {
                var line = await db.MeterReadings
                    .Include(r => r.OperationalSubmission)
                    .Where(r => r.Id == lineId
                                && accessibleSiteIds.Contains(r.OperationalSubmission.SubStationId))
                    .SingleOrDefaultAsync(cancellationToken);
                if (line is null) return Result.NotFound("Meter reading not found.");
                if (!ReadingApprovalPolicy.CanApprove(line.Status)) return Result.Success();
                var settings = await GetSettingsAsync(line.OperationalSubmission.SubStationId, cancellationToken);
                submittedBy = line.OperationalSubmission.SubmittedByUserId ?? line.OperationalSubmission.CreatedBy;
                if ((settings?.RequiresDifferentReviewer ?? true) && submittedBy == userId)
                    return Result.Failure("The reviewer must differ from the submitter (D18).", 403);
                line.Status = OperationalReadingStatus.Rejected;
                line.RejectedAt = now;
                line.RejectedByUserId = userId;
                line.RejectionReason = dto.Reason.Trim();
                submissionId = line.OperationalSubmissionId;
                break;
            }
            default:
                return Result.Failure($"Unknown line type '{lineType}'.");
        }

        await db.SaveChangesAsync(cancellationToken);

        // If all lines are now rejected, mark the submission rejected too
        await UpdateSubmissionStatusIfAllRejectedAsync(submissionId, now, userId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        // Notify submitter on rejection
        var sub = await db.OperationalSubmissions.AsNoTracking()
            .Where(s => s.Id == submissionId).Select(s => new { s.SubStationId, s.SubmittedByUserId, s.CreatedBy })
            .SingleOrDefaultAsync(cancellationToken);
        if (sub is not null)
        {
            int notifyUser = sub.SubmittedByUserId ?? sub.CreatedBy;
            await NotifySubmitterAsync(new OperationalSubmission { Id = submissionId, SubStationId = sub.SubStationId },
                notifyUser, "rejected", submissionId, cancellationToken);
        }

        return Result.Success();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task UpdateSubmissionStatusIfAllRejectedAsync(
        int submissionId, DateTime now, int userId, CancellationToken ct)
    {
        bool anyNonRejected =
            await db.OperationalGeneratorReadings.AnyAsync(r => r.OperationalSubmissionId == submissionId && r.Status != OperationalReadingStatus.Rejected, ct)
         || await db.TankLevelReadings.AnyAsync(r => r.OperationalSubmissionId == submissionId && r.Status != OperationalReadingStatus.Rejected, ct)
         || await db.FeederLoadReadings.AnyAsync(r => r.OperationalSubmissionId == submissionId && r.Status != OperationalReadingStatus.Rejected, ct)
         || await db.MeterReadings.AnyAsync(r => r.OperationalSubmissionId == submissionId && r.Status != OperationalReadingStatus.Rejected, ct);

        if (!anyNonRejected)
        {
            var sub = await db.OperationalSubmissions.FirstOrDefaultAsync(s => s.Id == submissionId, ct);
            if (sub is not null && sub.Status == OperationalReadingStatus.Pending)
            {
                sub.Status = OperationalReadingStatus.Rejected;
                sub.UpdatedAt = now;
                sub.UpdatedBy = userId;
            }
        }
    }

    private async Task<Dictionary<int, List<int>>> GetLinePhotoIdsAsync(
        string entityType, List<int> entityIds, CancellationToken ct)
    {
        if (entityIds.Count == 0) return [];
        var links = await db.AttachmentLinks
            .AsNoTracking()
            .Where(l => l.EntityType == entityType && entityIds.Contains(l.EntityId))
            .Select(l => new { l.EntityId, l.AttachmentId })
            .ToListAsync(ct);

        return links.GroupBy(l => l.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(l => l.AttachmentId).ToList());
    }

    private async Task<HashSet<int>> GetReuseWarningSetAsync(
        Dictionary<int, List<int>> linePhotoIds, int submissionId, CancellationToken ct)
    {
        var allPhotoIds = linePhotoIds.Values.SelectMany(x => x).Distinct().ToList();
        if (allPhotoIds.Count == 0) return [];

        // Find attachments linked to OTHER submissions (not this one) — reuse warning
        var reuseIds = await db.AttachmentLinks
            .AsNoTracking()
            .Where(l => allPhotoIds.Contains(l.AttachmentId)
                        && l.EntityType == "OperationalSubmission"
                        && l.EntityId != submissionId)
            .Select(l => l.AttachmentId)
            .Distinct()
            .ToListAsync(ct);

        return [.. reuseIds];
    }

    private async Task<HashSet<string>> GetSubmissionPhotoHashesAsync(int submissionId, CancellationToken ct)
    {
        var linkIds = await db.AttachmentLinks
            .AsNoTracking()
            .Where(l => l.EntityType == "OperationalSubmission" && l.EntityId == submissionId)
            .Select(l => l.AttachmentId)
            .ToListAsync(ct);

        if (linkIds.Count == 0) return [];
        var hashes = await db.Attachments.AsNoTracking()
            .Where(a => linkIds.Contains(a.Id))
            .Select(a => a.Sha256)
            .ToListAsync(ct);
        return [.. hashes];
    }

    private async Task<StationOperationalSettings?> GetSettingsAsync(int subStationId, CancellationToken ct)
    {
        var stationId = await db.SubStations.AsNoTracking()
            .Where(s => s.Id == subStationId).Select(s => s.StationId).FirstOrDefaultAsync(ct);
        return await db.StationOperationalSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.StationId == stationId, ct);
    }

    private async Task NotifySubmitterAsync(
        OperationalSubmission submission, int submittedByUserId,
        string action, int submissionId, CancellationToken ct)
    {
        try
        {
            await notifications.CreateAsync(new CreateNotificationDto(
                submission.SubStationId,
                action == "approved"
                    ? "تمت الموافقة على تسليمك"
                    : "تم رفض تسليمك",
                action == "approved"
                    ? $"تمت الموافقة على التسليم رقم {submissionId}."
                    : $"تم رفض سطر في التسليم رقم {submissionId}. يرجى مراجعة السبب وإعادة الإرسال.",
                "OperationalReview",
                action == "approved" ? "Info" : "Warning",
                [submittedByUserId],
                "OperationalSubmission",
                submissionId), null);
        }
        catch
        {
            // Notification failure must not block approval/rejection
        }
    }
}
