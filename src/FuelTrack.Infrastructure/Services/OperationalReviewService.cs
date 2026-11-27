using FuelTrack.Application.Common;
using FuelTrack.Application.Constants;
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
    IServiceDueEvaluationService? serviceDueEvaluation = null,
    ILogger<OperationalReviewService>? logger = null,
    ICommercialStationModeService? commercialMode = null) : IOperationalReviewService
{
    private sealed class ReviewTransactionResultException(Result result) : Exception
    {
        public Result Result { get; } = result;
    }

    private sealed record ApprovalCommit(
        OperationalSubmission Submission,
        int SubmittedBy,
        List<TankLevelReading> TankLines,
        List<MeterReading> MeterLines);
    private sealed record BundleApprovalCommit(
        OperationalSubmission Submission,
        int SubmittedBy,
        List<TankLevelReading> TankLines,
        List<MeterReading> MeterLines);
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
                // A single end-of-day submission always belongs to one business
                // day.  Prefer meter dates, then fall back to the other captured
                // reading types so the review queue can group a station's work by
                // its actual business day rather than the upload time.
                BusinessDate = s.MeterReadings.Where(r => r.DeletedAt == null)
                    .Select(r => (DateOnly?)r.BusinessDate).FirstOrDefault()
                    ?? s.GeneratorReadings.Where(r => r.DeletedAt == null)
                        .Select(r => (DateOnly?)r.BusinessDate).FirstOrDefault()
                    ?? s.TankLevelReadings.Where(r => r.DeletedAt == null)
                        .Select(r => (DateOnly?)r.BusinessDate).FirstOrDefault()
                    ?? s.FeederLoadReadings.Where(r => r.DeletedAt == null)
                        .Select(r => (DateOnly?)r.BusinessDate).FirstOrDefault(),
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
                r.Id, r.SubStationId, r.SiteName,
                r.BusinessDate ?? DateOnly.FromDateTime(r.ReceivedAtUtc), r.ReceivedAtUtc,
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
                r.RejectionReason, r.IsRunning, r.GeneratorId, GeneratorName = r.Generator.Name,
                r.LineVoltageVolts, r.CurrentAmps, r.ActiveLoadKw, r.ApparentLoadKva,
                r.FrequencyHz, r.PowerFactor, r.OilPressure, r.OilTemperatureCelsius,
                r.WaterTemperatureCelsius, r.PeakLoadKw,
                r.OilAddedLitres, r.OilId,
                OilName = db.Oils.Where(o => o.Id == r.OilId).Select(o => o.OilLookup.Name).FirstOrDefault(),
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
            new { r.GeneratorId, r.GeneratorName, r.IsRunning, r.LineVoltageVolts, r.CurrentAmps, r.ActiveLoadKw, r.ApparentLoadKva, r.FrequencyHz, r.PowerFactor, r.OilPressure, r.OilTemperatureCelsius, r.WaterTemperatureCelsius, r.PeakLoadKw, r.OilAddedLitres, r.OilId, r.OilName }
        )).ToList();

        // Tank lines
        var tankLines = await db.TankLevelReadings
            .AsNoTracking()
            .Where(r => r.OperationalSubmissionId == submissionId)
            .Select(r => new
            {
                r.Id, r.Status, r.CapturedAtUtc, r.ReceivedAtUtc,
                r.FuelTankId, FuelTankName = r.FuelTank.Name, GeneratorId = r.FuelTank.GeneratorId,
                r.LiquidLevelCm, r.ComputedLitres, r.EventType,
                r.BeforeLiquidLevelCm, r.BeforeComputedLitres, r.DeliveryNoteLitres,
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
            new { r.FuelTankId, r.FuelTankName, r.GeneratorId, r.LiquidLevelCm, r.ComputedLitres, r.EventType, r.BeforeLiquidLevelCm, r.BeforeComputedLitres, r.DeliveryNoteLitres }
        )).ToList();

        // Feeder lines
        var feederLines = await db.FeederLoadReadings
            .AsNoTracking()
            .Where(r => r.OperationalSubmissionId == submissionId)
            .Select(r => new
            {
                r.Id, r.Status, r.CapturedAtUtc, r.ReceivedAtUtc,
                r.FeederId, FeederName = r.Feeder.Name,
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
            new { r.FeederId, r.FeederName, r.ActiveLoadKw, r.ApparentLoadKva }
        )).ToList();

        // Meter lines with delta preview
        var meterLines = await db.MeterReadings
            .AsNoTracking()
            .Where(r => r.OperationalSubmissionId == submissionId)
            .ToListAsync(cancellationToken);

        var meterPhotoIds = await GetLinePhotoIdsAsync("MeterReading",
            meterLines.Select(m => m.Id).ToList(), cancellationToken);
        var meterReuseWarnings = await GetReuseWarningSetAsync(meterPhotoIds, submissionId, cancellationToken);

        var generatorMeterSubjectIds = meterLines
            .Where(r => r.SubjectType is MeterReadingSubjectType.GeneratorEnergyProduced or MeterReadingSubjectType.GeneratorRunningHours)
            .Select(r => r.SubjectId).Distinct().ToList();
        var feederMeterSubjectIds = meterLines
            .Where(r => r.SubjectType == MeterReadingSubjectType.FeederEnergySent)
            .Select(r => r.SubjectId).Distinct().ToList();
        var solarMeterSubjectIds = meterLines
            .Where(r => r.SubjectType == MeterReadingSubjectType.SolarEnergyProduced)
            .Select(r => r.SubjectId).Distinct().ToList();
        var generatorNames = await db.Generators.AsNoTracking().Where(g => generatorMeterSubjectIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken);
        var feederNames = await db.Feeders.AsNoTracking().Where(f => feederMeterSubjectIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.Name, cancellationToken);
        var solarMeterNames = await db.Meters.AsNoTracking().Where(m => solarMeterSubjectIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name, cancellationToken);

        var meterDtos = new List<ReviewMeterLineDto>();
        foreach (var r in meterLines)
        {
            // The submitted line is still Pending, so GetDailyDeltaAsync (which
            // intentionally reads official daily closings only) cannot use it as
            // the current value. Build a review-only preview from this line and
            // its latest preceding official closing instead.
            MeterDeltaDto? delta = r.Kind == MeterReadingKind.DailyClosing
                ? await GetReviewMeterDeltaAsync(r, submission.SubStationId, cancellationToken)
                : null;

            meterDtos.Add(new ReviewMeterLineDto(
                r.Id, r.Status.ToString(), r.SubjectType, r.SubjectId, r.Kind,
                r.CapturedAtUtc, r.ReceivedAtUtc, r.BusinessDate,
                (r.ReceivedAtUtc - r.CapturedAtUtc).Duration() > SkewThreshold,
                meterPhotoIds.GetValueOrDefault(r.Id, []),
                r.RawValue, r.MultiplierSnapshot, r.IsMeterReset, r.ResetNote,
                r.WarningsJson, r.RejectionReason,
                delta?.PreviousValue,
                delta?.Delta,
                delta?.SpanDays,
                r.SubjectType switch
                {
                    MeterReadingSubjectType.GeneratorEnergyProduced or MeterReadingSubjectType.GeneratorRunningHours => generatorNames.GetValueOrDefault(r.SubjectId),
                    MeterReadingSubjectType.FeederEnergySent => feederNames.GetValueOrDefault(r.SubjectId),
                    MeterReadingSubjectType.SolarEnergyProduced => solarMeterNames.GetValueOrDefault(r.SubjectId),
                    _ => null
                }));
        }

        List<DailyGeneratorFuelConsumptionDto>? dailyConsumptions = null;
        var hasDailyClosing = meterLines.Any(r => r.Kind == MeterReadingKind.DailyClosing);
        if (hasDailyClosing)
        {
            var businessDate = meterLines.FirstOrDefault(r => r.Kind == MeterReadingKind.DailyClosing)?.BusinessDate
                ?? DateOnly.FromDateTime(submission.ReceivedAtUtc);

            var siteGeneratorIds = await db.Generators.AsNoTracking()
                .Where(g => g.SubStationId == submission.SubStationId && g.IsActive)
                .Select(g => g.Id)
                .ToListAsync(cancellationToken);

            dailyConsumptions = new List<DailyGeneratorFuelConsumptionDto>();
            foreach (var gId in siteGeneratorIds)
            {
                var consRes = await fuelLoadSafety.GetDailyConsumptionAsync(gId, businessDate, userId, cancellationToken);
                if (consRes.IsSuccess && consRes.Value != null)
                {
                    // The safety service intentionally reads only official records.  A reviewer,
                    // however, must see the submitted end-of-day tank level and energy before
                    // approving it, without allowing that pending value to affect stock yet.
                    var official = consRes.Value;
                    var submittedTankLines = tankLines
                        .Where(line => line.GeneratorId == gId)
                        .OrderBy(line => line.CapturedAtUtc)
                        .ThenBy(line => line.Id)
                        .ToList();
                    var submittedEnergy = meterDtos
                        .Where(line => line.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced
                            && line.SubjectId == gId
                            && line.Kind == MeterReadingKind.DailyClosing)
                        .OrderBy(line => line.CapturedAtUtc)
                        .ThenBy(line => line.Id)
                        .LastOrDefault();

                    if (submittedTankLines.Count == 0)
                    {
                        dailyConsumptions.Add(official with
                        {
                            ProducedKwh = submittedEnergy?.DeltaPreview ?? official.ProducedKwh,
                            KwhPerLiter = official.ConsumptionLitres > 0 && (submittedEnergy?.DeltaPreview ?? official.ProducedKwh) is decimal officialProduced
                                ? Math.Round(officialProduced / official.ConsumptionLitres, 2)
                                : official.KwhPerLiter
                        });
                        continue;
                    }

                    var firstTankLine = submittedTankLines[0];
                    var lastTankLine = submittedTankLines[^1];
                    var opening = official.OpeningBalanceLitres != 0m
                        ? official.OpeningBalanceLitres
                        : firstTankLine.BeforeComputedLitres ?? firstTankLine.ComputedLitres;
                    var inbound = official.InboundRefillsLitres;
                    foreach (var tankLine in submittedTankLines.Where(line => line.EventType is TankEventType.Receipt or TankEventType.TransferIn))
                    {
                        var added = tankLine.ComputedLitres - (tankLine.BeforeComputedLitres ?? tankLine.ComputedLitres);
                        inbound += added > 0m
                            ? added
                            : tankLine.DeliveryNoteLitres.GetValueOrDefault() > 0m
                                ? tankLine.DeliveryNoteLitres.Value
                                : 0m;
                    }

                    var closing = lastTankLine.ComputedLitres;
                    var consumption = Math.Max(0m, opening + inbound - closing);
                    var produced = submittedEnergy?.DeltaPreview ?? official.ProducedKwh;
                    dailyConsumptions.Add(official with
                    {
                        DailyTankId = lastTankLine.FuelTankId,
                        DailyTankName = lastTankLine.FuelTankName,
                        OpeningBalanceLitres = opening,
                        InboundRefillsLitres = inbound,
                        ClosingBalanceLitres = closing,
                        ConsumptionLitres = consumption,
                        ProducedKwh = produced,
                        KwhPerLiter = consumption > 0m && produced.HasValue && produced.Value > 0m
                            ? Math.Round(produced.Value / consumption, 2)
                            : null
                    });
                }
            }
        }

        return Result<SubmissionDetailDto>.Success(new SubmissionDetailDto(
            submission.Id, submission.SubStationId, submission.SiteName,
            submission.ReceivedAtUtc,
            submission.SubmittedByUserId ?? 0, submission.SubmitterName,
            submission.Status.ToString(),
            genDtos, tankDtos, feederDtos, meterDtos,
            dailyConsumptions));
    }

    private async Task<MeterDeltaDto> GetReviewMeterDeltaAsync(
        MeterReading submittedReading,
        int subStationId,
        CancellationToken cancellationToken)
    {
        if (submittedReading.IsMeterReset)
        {
            return new MeterDeltaDto(
                submittedReading.BusinessDate, null, null, submittedReading.RawValue, null, true);
        }

        var previous = await db.MeterReadings
            .AsNoTracking()
            .Where(r => r.SubjectType == submittedReading.SubjectType
                        && r.SubjectId == submittedReading.SubjectId
                        && r.Kind == MeterReadingKind.DailyClosing
                        && (r.CapturedAtUtc < submittedReading.CapturedAtUtc
                            || (r.CapturedAtUtc == submittedReading.CapturedAtUtc && r.Id < submittedReading.Id))
                        && ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .OrderByDescending(r => r.CapturedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (previous is null)
        {
            var generatorIds = submittedReading.SubjectType is MeterReadingSubjectType.GeneratorEnergyProduced
                or MeterReadingSubjectType.GeneratorRunningHours
                ? new[] { submittedReading.SubjectId }
                : Array.Empty<int>();
            var feeders = submittedReading.SubjectType == MeterReadingSubjectType.FeederEnergySent
                ? await db.Feeders.AsNoTracking()
                    .Where(f => f.Id == submittedReading.SubjectId && f.SubStationId == subStationId)
                    .Select(f => new PreviousReadingBaselineResolver.FeederBaselineInfo(f.Id, f.MeterId, f.MeterMultiplier))
                    .ToListAsync(cancellationToken)
                : [];
            var solarMeterIds = submittedReading.SubjectType == MeterReadingSubjectType.SolarEnergyProduced
                ? new[] { submittedReading.SubjectId }
                : Array.Empty<int>();
            var resolver = new PreviousReadingBaselineResolver();
            var baselines = await resolver.LoadAsync(db, subStationId, generatorIds, feeders, solarMeterIds, cancellationToken);
            var baseline = resolver.ResolveSubject(
                submittedReading.SubjectType,
                submittedReading.SubjectId,
                Array.Empty<MeterReading>(),
                baselines).LatestOfficialDailyClosing;

            if (baseline is null)
                return new MeterDeltaDto(
                    submittedReading.BusinessDate, null, null, submittedReading.RawValue, null, false);

            return new MeterDeltaDto(
                submittedReading.BusinessDate,
                (submittedReading.RawValue - baseline.RawValue) * submittedReading.MultiplierSnapshot,
                baseline.RawValue,
                submittedReading.RawValue,
                null,
                false);
        }

        var span = submittedReading.BusinessDate.DayNumber - previous.BusinessDate.DayNumber;
        return new MeterDeltaDto(
            submittedReading.BusinessDate,
            (submittedReading.RawValue - previous.RawValue) * submittedReading.MultiplierSnapshot,
            previous.RawValue,
            submittedReading.RawValue,
            span > 1 ? span : null,
            false);
    }

    // ── Approve ──────────────────────────────────────────────────────────────

    public async Task<Result> ApproveSubmissionAsync(
        int submissionId, int userId, CancellationToken cancellationToken = default)
    {
        // Commercial end-of-day data must be reviewed per generator or feeder.
        // Keep this legacy whole-submission endpoint for enterprise callers only.
        if (commercialMode is not null && await commercialMode.IsCommercialModeAsync(cancellationToken))
            return Result.Failure("Commercial submissions must be reviewed per generator or feeder.", 422);

        var accessibleSiteIds = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);

        ApprovalCommit committed;
        try
        {
            committed = await DbTransactionRunner.RunAsync(db, async _ =>
            {

        var submission = await db.OperationalSubmissions
            .Where(s => s.Id == submissionId && accessibleSiteIds.Contains(s.SubStationId))
            .SingleOrDefaultAsync(cancellationToken);

        if (submission is null)
            throw new ReviewTransactionResultException(Result.NotFound("Submission not found or outside your scope."));

        // Idempotent: already fully approved
        if (submission.Status == OperationalReadingStatus.Approved)
            throw new ReviewTransactionResultException(Result.Success());

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
            throw new ReviewTransactionResultException(Result.Failure("The approver must be different from the submitter (D18).", 403));

        var now = DateTime.UtcNow;

        // Approve all non-rejected Pending lines
        var genLines = await db.OperationalGeneratorReadings
            .Where(r => r.OperationalSubmissionId == submissionId
                        && r.Status == OperationalReadingStatus.Pending)
            .ToListAsync(cancellationToken);

        // A submitted end-of-day oil top-up is commercial reviewable data, not
        // an immediate stock mutation. Only this approval transaction creates
        // its official consumption movement and deducts the selected balance.
        // Enterprise keeps its pre-commercial approval behaviour unchanged.
        var isCommercialMode = commercialMode is not null
            && await commercialMode.IsCommercialModeAsync(cancellationToken);
        var oilTopUps = isCommercialMode
            ? genLines.Where(r => r.OilAddedLitres.GetValueOrDefault() > 0m).ToList()
            : [];
        var oilTransactionsByReading = new Dictionary<OperationalGeneratorReading, OilTransaction>();
        if (oilTopUps.Count > 0)
        {
            if (oilTopUps.Any(r => !r.OilId.HasValue))
                throw new ReviewTransactionResultException(Result.Failure("An approved oil top-up requires a selected oil type.", 422));

            var selectedOilIds = oilTopUps.Select(r => r.OilId!.Value).Distinct().ToList();
            var oilsById = await db.Oils
                .Where(o => selectedOilIds.Contains(o.Id) && o.SubStationId == submission.SubStationId)
                .ToDictionaryAsync(o => o.Id, cancellationToken);
            if (oilsById.Count != selectedOilIds.Count)
                throw new ReviewTransactionResultException(Result.Failure("One or more selected oils are unavailable at this station.", 422));

            var consumptionTypeId = await db.Lookups
                .Where(l => l.Code == OilTransactionTypeCodes.Consumption
                    && l.IsActive
                    && (l.Category.Name == "OilTransactionType" || l.CategoryId == 100))
                .Select(l => (int?)l.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (!consumptionTypeId.HasValue)
                throw new ReviewTransactionResultException(Result.Failure("Oil consumption transaction type is not configured.", 422));

            foreach (var reading in oilTopUps)
            {
                var quantity = reading.OilAddedLitres!.Value;
                var oil = oilsById[reading.OilId!.Value];
                if (oil.CurrentStock < quantity)
                    throw new ReviewTransactionResultException(Result.Failure($"Insufficient stock for oil '{oil.OilLookup?.Name ?? oil.Id.ToString()}'.", 422));

                oil.CurrentStock -= quantity;
                oil.UpdatedAt = now;
                oil.UpdatedBy = userId;

                var transaction = new OilTransaction
                {
                    SubStationId = submission.SubStationId,
                    OilLookupId = oil.OilLookupId,
                    TransactionTypeLookupId = consumptionTypeId.Value,
                    TransactionDate = reading.CapturedAtUtc,
                    QuantityLiters = (double)quantity,
                    Reference = $"EOD-{submission.Id}-G{reading.GeneratorId}-{reading.ClientLineId:N}",
                    Notes = $"End-of-day generator oil top-up: {reading.GeneratorId}",
                    IsApproved = true,
                    ApprovedById = userId,
                    ApprovedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = userId,
                    UpdatedBy = userId
                };
                db.OilTransactions.Add(transaction);
                oilTransactionsByReading.Add(reading, transaction);
            }
        }

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
                throw new ReviewTransactionResultException(Result.Failure(monoResult.Error!, 422));
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
        foreach (var (reading, transaction) in oilTransactionsByReading)
            reading.OilTransactionId = transaction.Id;
        if (oilTransactionsByReading.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
        return new ApprovalCommit(submission, submittedBy, tankLines, meterLines);
            }, cancellationToken);
        }
        catch (ReviewTransactionResultException result)
        {
            return result.Result;
        }

        var submission = committed.Submission;
        var submittedBy = committed.SubmittedBy;
        var tankLines = committed.TankLines;
        var meterLines = committed.MeterLines;

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

        if (serviceDueEvaluation != null)
        {
            var genRunningHourIds = meterLines
                .Where(r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours)
                .Select(r => r.SubjectId)
                .Distinct()
                .ToList();

            foreach (var genId in genRunningHourIds)
            {
                try { await serviceDueEvaluation.EvaluateGeneratorAsync(genId, cancellationToken); }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Service-due evaluation failed after approving running hours for generator {GeneratorId}", genId);
                }
            }
        }

        // Notify submitter
        await NotifySubmitterAsync(submission, submittedBy, "approved", submissionId, cancellationToken);

        return Result.Success();
    }

    // ── Reject line ──────────────────────────────────────────────────────────

    public async Task<Result> ApproveBundleAsync(
        int submissionId, string bundleType, int subjectId, int userId,
        CancellationToken cancellationToken = default)
    {
        if (subjectId <= 0)
            return Result.Failure("A valid review subject is required.", 422);
        if (commercialMode is null || !await commercialMode.IsCommercialModeAsync(cancellationToken))
            return Result.NotFound("Bundle review is available only in commercial station mode.");

        var accessibleSiteIds = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);
        BundleApprovalCommit committed;
        try
        {
            committed = await DbTransactionRunner.RunAsync(db, async _ =>
            {
                var submission = await db.OperationalSubmissions
                    .SingleOrDefaultAsync(s => s.Id == submissionId
                        && accessibleSiteIds.Contains(s.SubStationId), cancellationToken);
                if (submission is null)
                    throw new ReviewTransactionResultException(Result.NotFound("Submission not found or outside your scope."));

                var submittedBy = submission.SubmittedByUserId ?? submission.CreatedBy;
                var settings = await GetSettingsAsync(submission.SubStationId, cancellationToken);
                if ((settings?.RequiresDifferentReviewer ?? true) && submittedBy == userId)
                    throw new ReviewTransactionResultException(Result.Failure("The reviewer must differ from the submitter (D18).", 403));

                var now = DateTime.UtcNow;
                var generatorLines = new List<OperationalGeneratorReading>();
                var tankLines = new List<TankLevelReading>();
                var feederLines = new List<FeederLoadReading>();
                var meterLines = new List<MeterReading>();

                switch (bundleType.Trim().ToLowerInvariant())
                {
                    case "generator":
                        generatorLines = await db.OperationalGeneratorReadings
                            .Where(r => r.OperationalSubmissionId == submissionId && r.GeneratorId == subjectId
                                && r.Status == OperationalReadingStatus.Pending)
                            .ToListAsync(cancellationToken);
                        tankLines = await db.TankLevelReadings
                            .Include(r => r.FuelTank)
                            .Where(r => r.OperationalSubmissionId == submissionId
                                && r.FuelTank.GeneratorId == subjectId
                                && r.Status == OperationalReadingStatus.Pending)
                            .ToListAsync(cancellationToken);
                        meterLines = await db.MeterReadings
                            .Where(r => r.OperationalSubmissionId == submissionId && r.SubjectId == subjectId
                                && (r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced
                                    || r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours)
                                && r.Status == OperationalReadingStatus.Pending)
                            .ToListAsync(cancellationToken);
                        break;
                    case "feeder":
                        feederLines = await db.FeederLoadReadings
                            .Where(r => r.OperationalSubmissionId == submissionId && r.FeederId == subjectId
                                && r.Status == OperationalReadingStatus.Pending)
                            .ToListAsync(cancellationToken);
                        meterLines = await db.MeterReadings
                            .Where(r => r.OperationalSubmissionId == submissionId && r.SubjectId == subjectId
                                && r.SubjectType == MeterReadingSubjectType.FeederEnergySent
                                && r.Status == OperationalReadingStatus.Pending)
                            .ToListAsync(cancellationToken);
                        break;
                    default:
                        throw new ReviewTransactionResultException(Result.Failure("Review bundle must be generator or feeder.", 422));
                }

                if (generatorLines.Count + tankLines.Count + feederLines.Count + meterLines.Count == 0)
                    throw new ReviewTransactionResultException(Result.Success());

                foreach (var meter in meterLines.Where(r => r.Kind == MeterReadingKind.DailyClosing))
                    await EnsureMeterIsMonotonicAsync(meter, cancellationToken);

                var oilTransactions = await ApplyCommercialOilTopUpsAsync(submission, generatorLines, userId, now, cancellationToken);
                foreach (var line in generatorLines) Approve(line, now, userId);
                foreach (var line in tankLines) Approve(line, now, userId);
                foreach (var line in feederLines) Approve(line, now, userId);
                foreach (var line in meterLines) Approve(line, now, userId);

                await db.SaveChangesAsync(cancellationToken);
                foreach (var (reading, transaction) in oilTransactions)
                    reading.OilTransactionId = transaction.Id;
                if (oilTransactions.Count > 0)
                    await db.SaveChangesAsync(cancellationToken);

                await UpdateSubmissionStatusIfReviewCompleteAsync(submission, now, userId, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                return new BundleApprovalCommit(submission, submittedBy, tankLines, meterLines);
            }, cancellationToken);
        }
        catch (ReviewTransactionResultException result)
        {
            return result.Result;
        }

        foreach (var line in committed.TankLines)
        {
            try { await fuelLoadSafety.EvaluateTankDeadStockAsync(line.Id, cancellationToken); }
            catch (Exception ex) { logger?.LogError(ex, "Dead-stock evaluation failed after approving tank reading {TankReadingId}", line.Id); }
        }
        if (serviceDueEvaluation is not null)
        {
            foreach (var generatorId in committed.MeterLines
                .Where(r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours)
                .Select(r => r.SubjectId).Distinct())
            {
                try { await serviceDueEvaluation.EvaluateGeneratorAsync(generatorId, cancellationToken); }
                catch (Exception ex) { logger?.LogError(ex, "Service-due evaluation failed after approving running hours for generator {GeneratorId}", generatorId); }
            }
        }

        await NotifySubmitterAsync(committed.Submission, committed.SubmittedBy, "approved", submissionId, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RejectBundleAsync(
        int submissionId, string bundleType, int subjectId, RejectLineDto dto, int userId,
        CancellationToken cancellationToken = default)
    {
        if (subjectId <= 0 || string.IsNullOrWhiteSpace(dto.Reason))
            return Result.Failure("A rejection reason and review subject are required.", 422);
        if (commercialMode is null || !await commercialMode.IsCommercialModeAsync(cancellationToken))
            return Result.NotFound("Bundle review is available only in commercial station mode.");

        var accessibleSiteIds = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);
        try
        {
            await DbTransactionRunner.RunAsync(db, async _ =>
            {
                var submission = await db.OperationalSubmissions
                    .SingleOrDefaultAsync(s => s.Id == submissionId
                        && accessibleSiteIds.Contains(s.SubStationId), cancellationToken);
                if (submission is null)
                    throw new ReviewTransactionResultException(Result.NotFound("Submission not found or outside your scope."));

                var submittedBy = submission.SubmittedByUserId ?? submission.CreatedBy;
                var settings = await GetSettingsAsync(submission.SubStationId, cancellationToken);
                if ((settings?.RequiresDifferentReviewer ?? true) && submittedBy == userId)
                    throw new ReviewTransactionResultException(Result.Failure("The reviewer must differ from the submitter (D18).", 403));

                var now = DateTime.UtcNow;
                var generatorLines = new List<OperationalGeneratorReading>();
                var tankLines = new List<TankLevelReading>();
                var feederLines = new List<FeederLoadReading>();
                var meterLines = new List<MeterReading>();
                switch (bundleType.Trim().ToLowerInvariant())
                {
                    case "generator":
                        generatorLines = await db.OperationalGeneratorReadings.Where(r => r.OperationalSubmissionId == submissionId && r.GeneratorId == subjectId && r.Status == OperationalReadingStatus.Pending).ToListAsync(cancellationToken);
                        tankLines = await db.TankLevelReadings.Where(r => r.OperationalSubmissionId == submissionId && r.FuelTank.GeneratorId == subjectId && r.Status == OperationalReadingStatus.Pending).ToListAsync(cancellationToken);
                        meterLines = await db.MeterReadings.Where(r => r.OperationalSubmissionId == submissionId && r.SubjectId == subjectId && (r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced || r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours) && r.Status == OperationalReadingStatus.Pending).ToListAsync(cancellationToken);
                        break;
                    case "feeder":
                        feederLines = await db.FeederLoadReadings.Where(r => r.OperationalSubmissionId == submissionId && r.FeederId == subjectId && r.Status == OperationalReadingStatus.Pending).ToListAsync(cancellationToken);
                        meterLines = await db.MeterReadings.Where(r => r.OperationalSubmissionId == submissionId && r.SubjectId == subjectId && r.SubjectType == MeterReadingSubjectType.FeederEnergySent && r.Status == OperationalReadingStatus.Pending).ToListAsync(cancellationToken);
                        break;
                    default:
                        throw new ReviewTransactionResultException(Result.Failure("Review bundle must be generator or feeder.", 422));
                }

                if (generatorLines.Count + tankLines.Count + feederLines.Count + meterLines.Count == 0)
                    throw new ReviewTransactionResultException(Result.Success());

                foreach (var line in generatorLines) Reject(line, dto.Reason, now, userId);
                foreach (var line in tankLines) Reject(line, dto.Reason, now, userId);
                foreach (var line in feederLines) Reject(line, dto.Reason, now, userId);
                foreach (var line in meterLines) Reject(line, dto.Reason, now, userId);
                await db.SaveChangesAsync(cancellationToken);
                await UpdateSubmissionStatusIfReviewCompleteAsync(submission, now, userId, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }, cancellationToken);
        }
        catch (ReviewTransactionResultException result)
        {
            return result.Result;
        }

        return Result.Success();
    }

    public async Task<Result> RejectLineAsync(
        string lineType, int lineId, RejectLineDto dto, int userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return Result.Failure("A rejection reason is required.", 422);

        var accessibleSiteIds = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);
        var now = DateTime.UtcNow;

        (int SubmissionId, int SubmittedBy) committed;
        try
        {
            committed = await DbTransactionRunner.RunAsync(db, async _ =>
            {
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
                if (line is null) throw new ReviewTransactionResultException(Result.NotFound("Generator reading not found."));
                if (!ReadingApprovalPolicy.CanApprove(line.Status)) throw new ReviewTransactionResultException(Result.Success()); // idempotent
                var settings = await GetSettingsAsync(line.OperationalSubmission.SubStationId, cancellationToken);
                submittedBy = line.OperationalSubmission.SubmittedByUserId ?? line.OperationalSubmission.CreatedBy;
                if ((settings?.RequiresDifferentReviewer ?? true) && submittedBy == userId)
                    throw new ReviewTransactionResultException(Result.Failure("The reviewer must differ from the submitter (D18).", 403));
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
                if (line is null) throw new ReviewTransactionResultException(Result.NotFound("Tank reading not found."));
                if (!ReadingApprovalPolicy.CanApprove(line.Status)) throw new ReviewTransactionResultException(Result.Success());
                var settings = await GetSettingsAsync(line.OperationalSubmission.SubStationId, cancellationToken);
                submittedBy = line.OperationalSubmission.SubmittedByUserId ?? line.OperationalSubmission.CreatedBy;
                if ((settings?.RequiresDifferentReviewer ?? true) && submittedBy == userId)
                    throw new ReviewTransactionResultException(Result.Failure("The reviewer must differ from the submitter (D18).", 403));
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
                if (line is null) throw new ReviewTransactionResultException(Result.NotFound("Feeder reading not found."));
                if (!ReadingApprovalPolicy.CanApprove(line.Status)) throw new ReviewTransactionResultException(Result.Success());
                var settings = await GetSettingsAsync(line.OperationalSubmission.SubStationId, cancellationToken);
                submittedBy = line.OperationalSubmission.SubmittedByUserId ?? line.OperationalSubmission.CreatedBy;
                if ((settings?.RequiresDifferentReviewer ?? true) && submittedBy == userId)
                    throw new ReviewTransactionResultException(Result.Failure("The reviewer must differ from the submitter (D18).", 403));
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
                if (line is null) throw new ReviewTransactionResultException(Result.NotFound("Meter reading not found."));
                if (!ReadingApprovalPolicy.CanApprove(line.Status)) throw new ReviewTransactionResultException(Result.Success());
                var settings = await GetSettingsAsync(line.OperationalSubmission.SubStationId, cancellationToken);
                submittedBy = line.OperationalSubmission.SubmittedByUserId ?? line.OperationalSubmission.CreatedBy;
                if ((settings?.RequiresDifferentReviewer ?? true) && submittedBy == userId)
                    throw new ReviewTransactionResultException(Result.Failure("The reviewer must differ from the submitter (D18).", 403));
                line.Status = OperationalReadingStatus.Rejected;
                line.RejectedAt = now;
                line.RejectedByUserId = userId;
                line.RejectionReason = dto.Reason.Trim();
                submissionId = line.OperationalSubmissionId;
                break;
            }
            default:
                throw new ReviewTransactionResultException(Result.Failure($"Unknown line type '{lineType}'."));
        }

        await db.SaveChangesAsync(cancellationToken);

        // If all lines are now rejected, mark the submission rejected too
        await UpdateSubmissionStatusIfAllRejectedAsync(submissionId, now, userId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return (submissionId, submittedBy);
            }, cancellationToken);
        }
        catch (ReviewTransactionResultException result)
        {
            return result.Result;
        }

        var submissionId = committed.SubmissionId;

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

    private async Task<List<(OperationalGeneratorReading Reading, OilTransaction Transaction)>> ApplyCommercialOilTopUpsAsync(
        OperationalSubmission submission,
        IReadOnlyCollection<OperationalGeneratorReading> generatorLines,
        int userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (commercialMode is null || !await commercialMode.IsCommercialModeAsync(cancellationToken))
            return [];

        var oilTopUps = generatorLines.Where(r => r.OilAddedLitres.GetValueOrDefault() > 0m).ToList();
        if (oilTopUps.Count == 0)
            return [];
        if (oilTopUps.Any(r => !r.OilId.HasValue))
            throw new ReviewTransactionResultException(Result.Failure("An approved oil top-up requires a selected oil type.", 422));

        var oilIds = oilTopUps.Select(r => r.OilId!.Value).Distinct().ToList();
        var oils = await db.Oils
            .Where(o => oilIds.Contains(o.Id) && o.SubStationId == submission.SubStationId)
            .ToDictionaryAsync(o => o.Id, cancellationToken);
        if (oils.Count != oilIds.Count)
            throw new ReviewTransactionResultException(Result.Failure("One or more selected oils are unavailable at this station.", 422));

        var consumptionTypeId = await db.Lookups
            .Where(l => l.Code == OilTransactionTypeCodes.Consumption
                && l.IsActive
                && (l.Category.Name == "OilTransactionType" || l.CategoryId == 100))
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!consumptionTypeId.HasValue)
            throw new ReviewTransactionResultException(Result.Failure("Oil consumption transaction type is not configured.", 422));

        var transactions = new List<(OperationalGeneratorReading Reading, OilTransaction Transaction)>();
        foreach (var reading in oilTopUps)
        {
            var quantity = reading.OilAddedLitres!.Value;
            var oil = oils[reading.OilId!.Value];
            if (oil.CurrentStock < quantity)
                throw new ReviewTransactionResultException(Result.Failure($"Insufficient stock for oil '{oil.OilLookup?.Name ?? oil.Id.ToString()}'.", 422));

            oil.CurrentStock -= quantity;
            oil.UpdatedAt = now;
            oil.UpdatedBy = userId;
            var transaction = new OilTransaction
            {
                SubStationId = submission.SubStationId,
                OilLookupId = oil.OilLookupId,
                TransactionTypeLookupId = consumptionTypeId.Value,
                TransactionDate = reading.CapturedAtUtc,
                QuantityLiters = (double)quantity,
                Reference = $"EOD-{submission.Id}-G{reading.GeneratorId}-{reading.ClientLineId:N}",
                Notes = $"End-of-day generator oil top-up: {reading.GeneratorId}",
                IsApproved = true,
                ApprovedById = userId,
                ApprovedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userId,
                UpdatedBy = userId
            };
            db.OilTransactions.Add(transaction);
            transactions.Add((reading, transaction));
        }
        return transactions;
    }

    private async Task EnsureMeterIsMonotonicAsync(MeterReading meter, CancellationToken cancellationToken)
    {
        var sem = SubjectLocks.GetOrAdd($"meter:{meter.SubjectType}:{meter.SubjectId}", _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(cancellationToken);
        try
        {
            var result = await meterDelta.ValidateMonotonicityAsync(meter.Id, cancellationToken);
            if (!result.IsSuccess)
                throw new ReviewTransactionResultException(Result.Failure(result.Error!, 422));
        }
        finally
        {
            sem.Release();
        }
    }

    private static void Approve(OperationalGeneratorReading line, DateTime now, int userId)
    {
        line.Status = OperationalReadingStatus.Approved;
        line.ApprovedAt = now;
        line.ApprovedByUserId = userId;
    }

    private static void Approve(TankLevelReading line, DateTime now, int userId)
    {
        line.Status = OperationalReadingStatus.Approved;
        line.ApprovedAt = now;
        line.ApprovedByUserId = userId;
    }

    private static void Approve(FeederLoadReading line, DateTime now, int userId)
    {
        line.Status = OperationalReadingStatus.Approved;
        line.ApprovedAt = now;
        line.ApprovedByUserId = userId;
    }

    private static void Approve(MeterReading line, DateTime now, int userId)
    {
        line.Status = OperationalReadingStatus.Approved;
        line.ApprovedAt = now;
        line.ApprovedByUserId = userId;
    }

    private static void Reject(OperationalGeneratorReading line, string reason, DateTime now, int userId)
    {
        line.Status = OperationalReadingStatus.Rejected;
        line.RejectedAt = now;
        line.RejectedByUserId = userId;
        line.RejectionReason = reason.Trim();
    }

    private static void Reject(TankLevelReading line, string reason, DateTime now, int userId)
    {
        line.Status = OperationalReadingStatus.Rejected;
        line.RejectedAt = now;
        line.RejectedByUserId = userId;
        line.RejectionReason = reason.Trim();
    }

    private static void Reject(FeederLoadReading line, string reason, DateTime now, int userId)
    {
        line.Status = OperationalReadingStatus.Rejected;
        line.RejectedAt = now;
        line.RejectedByUserId = userId;
        line.RejectionReason = reason.Trim();
    }

    private static void Reject(MeterReading line, string reason, DateTime now, int userId)
    {
        line.Status = OperationalReadingStatus.Rejected;
        line.RejectedAt = now;
        line.RejectedByUserId = userId;
        line.RejectionReason = reason.Trim();
    }

    private async Task UpdateSubmissionStatusIfReviewCompleteAsync(
        OperationalSubmission submission, DateTime now, int userId, CancellationToken ct)
    {
        var submissionId = submission.Id;
        var pending = await db.OperationalGeneratorReadings.AnyAsync(r => r.OperationalSubmissionId == submissionId && (r.Status == OperationalReadingStatus.Pending || r.Status == OperationalReadingStatus.AwaitingPhoto), ct)
            || await db.TankLevelReadings.AnyAsync(r => r.OperationalSubmissionId == submissionId && (r.Status == OperationalReadingStatus.Pending || r.Status == OperationalReadingStatus.AwaitingPhoto), ct)
            || await db.FeederLoadReadings.AnyAsync(r => r.OperationalSubmissionId == submissionId && (r.Status == OperationalReadingStatus.Pending || r.Status == OperationalReadingStatus.AwaitingPhoto), ct)
            || await db.MeterReadings.AnyAsync(r => r.OperationalSubmissionId == submissionId && (r.Status == OperationalReadingStatus.Pending || r.Status == OperationalReadingStatus.AwaitingPhoto), ct);
        if (pending)
            return;

        var hasApproved = await db.OperationalGeneratorReadings.AnyAsync(r => r.OperationalSubmissionId == submissionId && r.Status == OperationalReadingStatus.Approved, ct)
            || await db.TankLevelReadings.AnyAsync(r => r.OperationalSubmissionId == submissionId && r.Status == OperationalReadingStatus.Approved, ct)
            || await db.FeederLoadReadings.AnyAsync(r => r.OperationalSubmissionId == submissionId && r.Status == OperationalReadingStatus.Approved, ct)
            || await db.MeterReadings.AnyAsync(r => r.OperationalSubmissionId == submissionId && r.Status == OperationalReadingStatus.Approved, ct);
        submission.Status = hasApproved ? OperationalReadingStatus.Approved : OperationalReadingStatus.Rejected;
        submission.UpdatedAt = now;
        submission.UpdatedBy = userId;
    }

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
