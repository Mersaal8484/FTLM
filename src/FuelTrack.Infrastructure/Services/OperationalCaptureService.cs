using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

/// <summary>Owns raw capture.  It applies the approval policy but never calculates official totals.</summary>
public sealed class OperationalCaptureService(
    FuelTrackDbContext db,
    IUserScopeFilterService scopeFilter,
    ICurrentUserService currentUser,
    IIdempotencyService idempotency,
    IFuelLoadSafetyService fuelLoadSafety,
    ICommercialStationModeService? commercialMode = null) : IOperationalCaptureService
{
    private const string Operation = "operational.capture.v1";
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SkewWarning = TimeSpan.FromHours(12);
    private readonly ICommercialStationModeService _commercialMode = commercialMode ?? new CommercialStationModeService(db);

    public async Task<Result<PreviousMeterReadingsDto>> GetPreviousReadingsAsync(int subStationId, int userId, CancellationToken cancellationToken = default)
    {
        if (subStationId <= 0)
        {
            var commercialScope = await _commercialMode.GetScopeAsync(cancellationToken);
            if (commercialScope is not null)
                subStationId = commercialScope.SubStationId;
        }

        if (subStationId <= 0 || !await CanAccessSiteAsync(userId, subStationId, cancellationToken))
            return Result<PreviousMeterReadingsDto>.NotFound("Site not found or outside your scope.");

        var generatorIds = await db.Generators.AsNoTracking()
            .Where(x => x.SubStationId == subStationId && x.DeletedAt == null)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var feeders = await db.Feeders.AsNoTracking()
            .Where(x => x.SubStationId == subStationId && x.DeletedAt == null)
            .Select(x => new PreviousReadingBaselineResolver.FeederBaselineInfo(x.Id, x.MeterId, x.MeterMultiplier))
            .ToListAsync(cancellationToken);
        var feederIds = feeders.Select(x => x.Id).ToList();
        var solarMeterIds = await db.Meters.AsNoTracking()
            .Where(x => x.SubStationId == subStationId && x.IsActive && x.DeletedAt == null &&
                        x.MeterTypeLookup != null &&
                        (x.MeterTypeLookup.Code == "SolarProductionMeter" || x.MeterTypeLookup.Code == "SolarMeter"))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var tanks = await db.FuelTanks.AsNoTracking()
            .Where(x => x.SubStationId == subStationId && x.DeletedAt == null)
            .Select(x => new { x.Id, x.ActualStockLiters })
            .ToListAsync(cancellationToken);
        var tankIds = tanks.Select(x => x.Id).ToList();

        var candidates = await db.MeterReadings.AsNoTracking()
            .Where(x => x.Status != OperationalReadingStatus.Voided && x.Status != OperationalReadingStatus.Rejected &&
                ((x.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced && generatorIds.Contains(x.SubjectId)) ||
                 (x.SubjectType == MeterReadingSubjectType.GeneratorRunningHours && generatorIds.Contains(x.SubjectId)) ||
                 (x.SubjectType == MeterReadingSubjectType.FeederEnergySent && feederIds.Contains(x.SubjectId)) ||
                 (x.SubjectType == MeterReadingSubjectType.SolarEnergyProduced && solarMeterIds.Contains(x.SubjectId))))
            .OrderByDescending(x => x.CapturedAtUtc).ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        var baselineResolver = new PreviousReadingBaselineResolver();
        var baselines = await baselineResolver.LoadAsync(db, subStationId, generatorIds, feeders, solarMeterIds, cancellationToken);

        var requiredSubjects = generatorIds.SelectMany(id => new[]
        {
            (SubjectType: MeterReadingSubjectType.GeneratorEnergyProduced, SubjectId: id),
            (SubjectType: MeterReadingSubjectType.GeneratorRunningHours, SubjectId: id),
        })
        .Concat(feederIds.Select(id => (SubjectType: MeterReadingSubjectType.FeederEnergySent, SubjectId: id)))
        .Concat(solarMeterIds.Select(id => (SubjectType: MeterReadingSubjectType.SolarEnergyProduced, SubjectId: id)));

        var readings = requiredSubjects.Select(subject =>
        {
            var subjectReadings = candidates.Where(x => x.SubjectType == subject.SubjectType && x.SubjectId == subject.SubjectId).ToList();
            return baselineResolver.ResolveSubject(subject.SubjectType, subject.SubjectId, subjectReadings, baselines);
        }).ToList();

        var stationId = await db.SubStations.AsNoTracking()
            .Where(x => x.Id == subStationId)
            .Select(x => x.StationId)
            .FirstOrDefaultAsync(cancellationToken);
        var timeZone = stationId > 0 ? await GetStationTimeZoneAsync(stationId, cancellationToken) : TimeZoneInfo.Utc;
        var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Date;
        var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(todayLocal, DateTimeKind.Unspecified), timeZone);

        var allOfficialTankReadings = tankIds.Count == 0
            ? []
            : await db.TankLevelReadings.AsNoTracking()
                .Where(x => tankIds.Contains(x.FuelTankId) &&
                            (x.Status == OperationalReadingStatus.Recorded || x.Status == OperationalReadingStatus.Approved))
                .OrderBy(x => x.CapturedAtUtc)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.FuelTankId,
                    x.LiquidLevelCm,
                    x.ComputedLitres,
                    x.BeforeComputedLitres,
                    x.EventType,
                    x.CapturedAtUtc
                })
                .ToListAsync(cancellationToken);

        var readingsByTank = allOfficialTankReadings
            .GroupBy(x => x.FuelTankId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var latestTankReadings = new List<PreviousTankLevelReadingDto>();
        var tankBalances = new List<TankDailyBalanceDto>();

        foreach (var tank in tanks)
        {
            var history = readingsByTank.GetValueOrDefault(tank.Id, []);
            var todayReadings = history.Where(r => r.CapturedAtUtc >= todayStartUtc).ToList();
            decimal? openingLitres = history
                .Where(r => r.CapturedAtUtc < todayStartUtc)
                .Select(r => (decimal?)r.ComputedLitres)
                .LastOrDefault()
                ?? todayReadings.FirstOrDefault()?.BeforeComputedLitres
                ?? (tank.ActualStockLiters.HasValue ? (decimal)tank.ActualStockLiters.Value : null);

            decimal transferInLitres = 0m;
            decimal? prevLitres = openingLitres;
            foreach (var r in todayReadings)
            {
                var before = r.BeforeComputedLitres ?? prevLitres ?? r.ComputedLitres;
                if (r.EventType == TankEventType.TransferIn)
                    transferInLitres += Math.Max(0m, r.ComputedLitres - before);
                prevLitres = r.ComputedLitres;
            }

            tankBalances.Add(new TankDailyBalanceDto(tank.Id, openingLitres, transferInLitres));

            var latestLevel = history.LastOrDefault(r => r.EventType == TankEventType.Level);
            if (latestLevel is not null)
            {
                latestTankReadings.Add(new PreviousTankLevelReadingDto(
                    latestLevel.FuelTankId,
                    latestLevel.LiquidLevelCm,
                    latestLevel.ComputedLitres,
                    latestLevel.CapturedAtUtc,
                    openingLitres,
                    transferInLitres));
            }
        }

        return Result<PreviousMeterReadingsDto>.Success(new PreviousMeterReadingsDto(readings, latestTankReadings, tankBalances));
    }

    public async Task<Result<OperationalSubmissionDto>> SubmitAsync(SubmitOperationalCaptureDto dto, int userId, CancellationToken cancellationToken = default)
    {
        if (dto.SubStationId <= 0)
        {
            var commercialScope = await _commercialMode.GetScopeAsync(cancellationToken);
            if (commercialScope is not null)
                dto = dto with { SubStationId = commercialScope.SubStationId };
        }

        if (dto.ClientRequestId == Guid.Empty || dto.SubStationId <= 0)
            return Result<OperationalSubmissionDto>.Failure("A site and client request ID are required.");
        var lineCount = dto.GeneratorReadings.Count + dto.TankLevelReadings.Count + dto.FeederLoadReadings.Count + dto.MeterReadings.Count;
        if (lineCount is 0 or > 50) return Result<OperationalSubmissionDto>.Failure("A submission must contain between one and fifty reading lines.");
        var lineIds = dto.GeneratorReadings.Select(x => x.ClientLineId)
            .Concat(dto.TankLevelReadings.Select(x => x.ClientLineId))
            .Concat(dto.FeederLoadReadings.Select(x => x.ClientLineId))
            .Concat(dto.MeterReadings.Select(x => x.ClientLineId))
            .ToList();
        if (lineIds.Any(x => x is null || x == Guid.Empty) || lineIds.Distinct().Count() != lineCount)
            return Result<OperationalSubmissionDto>.Failure("Each reading line requires a distinct device-generated client line ID.");
        if (!await CanAccessSiteAsync(userId, dto.SubStationId, cancellationToken))
            return Result<OperationalSubmissionDto>.NotFound("Site not found or outside your scope.");

        var requestHash = Hash(dto);
        var idempotencyStart = await idempotency.StartAsync(dto.ClientRequestId, Operation, requestHash, userId, cancellationToken);
        if (!idempotencyStart.IsSuccess) return Result<OperationalSubmissionDto>.Failure(idempotencyStart.Error!, idempotencyStart.StatusCode);
        if (!idempotencyStart.Value!.IsNew)
        {
            if (idempotencyStart.Value.ResponseStatusCode is int status && !string.IsNullOrWhiteSpace(idempotencyStart.Value.ResponsePayload))
            {
                var saved = JsonSerializer.Deserialize<OperationalSubmissionDto>(idempotencyStart.Value.ResponsePayload);
                if (saved is not null) return Result<OperationalSubmissionDto>.Success(saved, status);
            }
            return Result<OperationalSubmissionDto>.Conflict("This request is still being processed. Retry shortly.");
        }

        try
        {
            var now = DateTime.UtcNow;
            var site = await db.SubStations.AsNoTracking().Where(x => x.Id == dto.SubStationId)
                .Select(x => new { x.Id, x.StationId }).SingleOrDefaultAsync(cancellationToken);
            if (site is null)
            {
                await idempotency.CompleteAsync(dto.ClientRequestId, 404, null, userId, cancellationToken);
                return Result<OperationalSubmissionDto>.NotFound("Site was not found.");
            }
            var settings = await db.StationOperationalSettings.AsNoTracking().SingleOrDefaultAsync(x => x.StationId == site.StationId, cancellationToken);
            var timeZone = await GetStationTimeZoneAsync(site.StationId, cancellationToken);
            var isCommercial = await _commercialMode.IsCommercialModeAsync(cancellationToken);
            var tier1RequiresApproval = !isCommercial && (settings?.Tier1RequiresApproval ?? false);
            var isDailyClosing = dto.MeterReadings.Any(x => x.Kind == MeterReadingKind.DailyClosing);
            var dailyTankCoverageError = await ValidateCommercialDailyTankCoverageAsync(dto, isCommercial, cancellationToken);
            if (dailyTankCoverageError is not null)
            {
                await idempotency.CompleteAsync(dto.ClientRequestId, 400, null, userId, cancellationToken);
                return Result<OperationalSubmissionDto>.Failure(dailyTankCoverageError);
            }
            var (newDto, duplicateLines) = await ExcludeKnownClientLinesAsync(dto, cancellationToken);
            var (captureDto, duplicateTimeLines) = await ExcludeDuplicateTimesAsync(newDto, now, cancellationToken);
            duplicateLines = duplicateLines.Concat(duplicateTimeLines).ToList();
            var (validatedDto, rejectedLines) = await ExcludeRejectedLinesAsync(captureDto, userId, site.Id, now, cancellationToken);
            duplicateLines = duplicateLines.Concat(rejectedLines).ToList();
            if (validatedDto.GeneratorReadings.Count + validatedDto.TankLevelReadings.Count + validatedDto.FeederLoadReadings.Count + validatedDto.MeterReadings.Count == 0)
            {
                var rejectedOnly = new OperationalSubmissionDto(0, dto.SubStationId, now, OperationalReadingStatus.Recorded.ToString(), 0, 0, 0, 0, duplicateLines);
                await idempotency.CompleteAsync(dto.ClientRequestId, 200, JsonSerializer.Serialize(rejectedOnly), userId, cancellationToken);
                return Result<OperationalSubmissionDto>.Success(rejectedOnly, 200);
            }
            var validation = await ValidateAsync(validatedDto, userId, site.Id, now, cancellationToken);
            if (validation is not null)
            {
                await idempotency.CompleteAsync(dto.ClientRequestId, 400, null, userId, cancellationToken);
                return Result<OperationalSubmissionDto>.Failure(validation);
            }

            OperationalSubmission? submission = null;
            await DbTransactionRunner.RunAsync(db, async _ =>
            {
            submission = new OperationalSubmission
            {
                SubStationId = dto.SubStationId,
                ClientRequestId = dto.ClientRequestId,
                SubmissionCapturedAtUtc = dto.SubmissionCapturedAtUtc is null ? null : NormalizeUtc(dto.SubmissionCapturedAtUtc.Value),
                ClientSentAtUtc = dto.ClientSentAtUtc is null ? null : NormalizeUtc(dto.ClientSentAtUtc.Value),
                ReceivedAtUtc = now,
                Status = OperationalReadingStatus.Recorded,
                SubmittedByUserId = userId,
                CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId,
            };
            db.OperationalSubmissions.Add(submission);

            dto = validatedDto;
            var allIncomingPhotoIds = dto.GeneratorReadings.SelectMany(x => x.PhotoAttachmentIds)
                .Concat(dto.TankLevelReadings.SelectMany(x => x.PhotoAttachmentIds).Concat(dto.TankLevelReadings.SelectMany(x => x.BeforePhotoAttachmentIds ?? [])))
                .Concat(dto.FeederLoadReadings.SelectMany(x => x.PhotoAttachmentIds)).Concat(dto.MeterReadings.SelectMany(x => x.PhotoAttachmentIds)).Distinct().ToList();
            var reusedPhotoIds = await db.AttachmentLinks.AsNoTracking().Where(x => allIncomingPhotoIds.Contains(x.AttachmentId))
                .Select(x => x.AttachmentId).Distinct().ToListAsync(cancellationToken);
            var attachmentLinks = new List<(int AttachmentId, object Entity)>();
            var tankEventsByClientLine = new Dictionary<Guid, TankLevelReading>();
            foreach (var line in dto.GeneratorReadings)
            {
                var captured = ResolveCapturedAt(line.CapturedAtOverrideUtc ?? line.CapturedAtUtc, dto.SubmissionCapturedAtUtc, now);
                var awaitingPhoto = dto.IsOfflineQueue && line.PhotoAttachmentIds.Count == 0;
                var requiresApproval = ReadingApprovalPolicy.RequiresApproval(OperationalSubjectType.Generator, tier1RequiresApproval: tier1RequiresApproval);
                var entity = new OperationalGeneratorReading
                {
                    OperationalSubmission = submission, GeneratorId = line.GeneratorId, CapturedAtUtc = captured, ReceivedAtUtc = now,
                    ClientLineId = line.ClientLineId ?? Guid.NewGuid(), BusinessDate = BusinessDate(captured, timeZone), Status = ReadingApprovalPolicy.InitialStatus(requiresApproval, awaitingPhoto), IsRunning = line.IsRunning,
                    LineVoltageVolts = line.LineVoltageVolts, CurrentAmps = line.CurrentAmps, WaterTemperatureCelsius = line.WaterTemperatureCelsius,
                    OilPressure = line.OilPressure, OilTemperatureCelsius = line.OilTemperatureCelsius, ActiveLoadKw = line.ActiveLoadKw,
                    ApparentLoadKva = line.ApparentLoadKva, ReactiveLoadKvar = line.ReactiveLoadKvar, FrequencyHz = line.FrequencyHz,
                    OilAddedLitres = line.OilAddedLitres, PowerFactor = line.PowerFactor, PeakLoadKw = line.PeakLoadKw, AirPressureBar = line.AirPressureBar,
                    WarningsJson = WarningJson(captured, now, dto.ClientSentAtUtc, line.PhotoAttachmentIds.Any(reusedPhotoIds.Contains)), CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId,
                };
                db.OperationalGeneratorReadings.Add(entity);
                attachmentLinks.AddRange(line.PhotoAttachmentIds.Select(id => (id, (object)entity)));
            }

            var tankCalibration = await db.FuelTanks.Where(x => dto.TankLevelReadings.Select(y => y.FuelTankId).Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);
            foreach (var line in dto.TankLevelReadings)
            {
                var tank = tankCalibration[line.FuelTankId];
                var captured = ResolveCapturedAt(line.CapturedAtOverrideUtc ?? line.CapturedAtUtc, dto.SubmissionCapturedAtUtc, now);
                var litres = TankLevelConverter.LitresFromLevel(tank, line.LiquidLevelCm);
                var beforeLitres = line.BeforeLiquidLevelCm is null ? (decimal?)null : TankLevelConverter.LitresFromLevel(tank, line.BeforeLiquidLevelCm.Value);
                var awaitingPhoto = dto.IsOfflineQueue && line.PhotoAttachmentIds.Count == 0;
                var requiresApproval = isDailyClosing || ReadingApprovalPolicy.RequiresApproval(OperationalSubjectType.Tank, eventType: line.EventType, tier1RequiresApproval: tier1RequiresApproval);
                var entity = new TankLevelReading
                {
                    OperationalSubmission = submission, FuelTankId = tank.Id, CapturedAtUtc = captured, ReceivedAtUtc = now,
                    ClientLineId = line.ClientLineId ?? Guid.NewGuid(), BusinessDate = BusinessDate(captured, timeZone), Status = ReadingApprovalPolicy.InitialStatus(requiresApproval, awaitingPhoto), LiquidLevelCm = line.LiquidLevelCm,
                    ComputedLitres = litres, ShapeSnapshot = tank.Shape ?? TankShape.VerticalCylinderWithConicalBottom, DiameterCmSnapshot = tank.DiameterCm ?? 0m,
                    CylinderHeightCmSnapshot = tank.CylinderHeightCm ?? 0m, ConeHeightCmSnapshot = tank.ConeHeightCm ?? 0m,
                    MultiplicationFactorSnapshot = tank.MultiplicationFactor ?? 1m, CalibrationModeSnapshot = tank.CalibrationMode, LitresPerCmSnapshot = tank.LitresPerCm,
                    EventType = line.EventType, BeforeLiquidLevelCm = line.BeforeLiquidLevelCm,
                    BeforeComputedLitres = beforeLitres, DeliveryNoteLitres = line.DeliveryNoteLitres, DeadStockLitresSnapshot = TankVolumeService.DeadStockLitres(tank),
                    WarningsJson = WarningJson(captured, now, dto.ClientSentAtUtc,
                        line.PhotoAttachmentIds.Any(reusedPhotoIds.Contains) || (line.BeforePhotoAttachmentIds ?? []).Any(reusedPhotoIds.Contains),
                        tank.GeneratorId.HasValue && TankVolumeService.IsAtOrBelowDeadStock(litres, TankVolumeService.DeadStockLitres(tank))),
                    CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId,
                };
                db.TankLevelReadings.Add(entity);
                attachmentLinks.AddRange(line.PhotoAttachmentIds.Select(id => (id, (object)entity)));
                attachmentLinks.AddRange((line.BeforePhotoAttachmentIds ?? []).Select(id => (id, (object)entity)));
                tankEventsByClientLine[entity.ClientLineId] = entity;
            }

            var feederMap = await db.Feeders.Where(x => dto.FeederLoadReadings.Select(y => y.FeederId).Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);
            foreach (var line in dto.FeederLoadReadings)
            {
                var feeder = feederMap[line.FeederId];
                var captured = ResolveCapturedAt(line.CapturedAtOverrideUtc ?? line.CapturedAtUtc, dto.SubmissionCapturedAtUtc, now);
                var awaitingPhoto = dto.IsOfflineQueue && line.PhotoAttachmentIds.Count == 0;
                var requiresApproval = ReadingApprovalPolicy.RequiresApproval(OperationalSubjectType.Feeder, tier1RequiresApproval: tier1RequiresApproval);
                var entity = new FeederLoadReading
                {
                    OperationalSubmission = submission, FeederId = feeder.Id, CapturedAtUtc = captured, ReceivedAtUtc = now,
                    ClientLineId = line.ClientLineId ?? Guid.NewGuid(), BusinessDate = BusinessDate(captured, timeZone), Status = ReadingApprovalPolicy.InitialStatus(requiresApproval, awaitingPhoto),
                    ActiveLoadKw = line.ActiveLoadKw, ApparentLoadKva = line.ApparentLoadKva, MeterMultiplierSnapshot = feeder.MeterMultiplier,
                    WarningsJson = WarningJson(captured, now, dto.ClientSentAtUtc, line.PhotoAttachmentIds.Any(reusedPhotoIds.Contains)), CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId,
                };
                db.FeederLoadReadings.Add(entity);
                attachmentLinks.AddRange(line.PhotoAttachmentIds.Select(id => (id, (object)entity)));
            }

            foreach (var line in dto.MeterReadings)
            {
                var captured = ResolveCapturedAt(line.CapturedAtOverrideUtc ?? line.CapturedAtUtc, dto.SubmissionCapturedAtUtc, now);
                var multiplier = line.MultiplierSnapshot ?? 1m;
                var awaitingPhoto = dto.IsOfflineQueue && line.PhotoAttachmentIds.Count == 0;
                var requiresApproval = ReadingApprovalPolicy.RequiresApproval(OperationalSubjectType.Meter, line.Kind, tier1RequiresApproval: tier1RequiresApproval);
                var entity = new MeterReading
                {
                    OperationalSubmission = submission, SubjectType = line.SubjectType, SubjectId = line.SubjectId, MeterId = line.MeterId,
                    Kind = line.Kind, CapturedAtUtc = captured, ReceivedAtUtc = now, ClientLineId = line.ClientLineId ?? Guid.NewGuid(), BusinessDate = line.BusinessDate ?? BusinessDate(captured, timeZone),
                    Status = ReadingApprovalPolicy.InitialStatus(requiresApproval, awaitingPhoto), RawValue = line.RawValue, MultiplierSnapshot = multiplier,
                    IsMeterReset = line.IsMeterReset, ResetNote = line.ResetNote?.Trim(), WarningsJson = WarningJson(captured, now, dto.ClientSentAtUtc, line.PhotoAttachmentIds.Any(reusedPhotoIds.Contains)),
                    CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId,
                };
                db.MeterReadings.Add(entity);
                attachmentLinks.AddRange(line.PhotoAttachmentIds.Select(id => (id, (object)entity)));
            }

            await db.SaveChangesAsync(cancellationToken);
            foreach (var line in dto.TankLevelReadings.Where(x => x.LinkedClientLineId.HasValue))
                if (tankEventsByClientLine.TryGetValue(line.ClientLineId ?? Guid.Empty, out var eventLine) && tankEventsByClientLine.TryGetValue(line.LinkedClientLineId!.Value, out var linked))
                    eventLine.LinkedEventId = linked.Id;
            var hasPendingReview = submission.GeneratorReadings.Any(x => x.Status == OperationalReadingStatus.Pending)
                || submission.TankLevelReadings.Any(x => x.Status == OperationalReadingStatus.Pending)
                || submission.FeederLoadReadings.Any(x => x.Status == OperationalReadingStatus.Pending)
                || submission.MeterReadings.Any(x => x.Status == OperationalReadingStatus.Pending);
            submission.Status = hasPendingReview ? OperationalReadingStatus.Pending : OperationalReadingStatus.Recorded;
            foreach (var (attachmentId, entity) in attachmentLinks)
                db.AttachmentLinks.Add(new AttachmentLink
                {
                    AttachmentId = attachmentId, EntityType = entity.GetType().Name, EntityId = ((BaseEntity)entity).Id,
                    SortOrder = 0, CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId,
                });
            await db.SaveChangesAsync(cancellationToken);
            });

            var committedSubmission = submission!;

            // Safety episodes are evaluated after capture is durable. Notification failures
            // cannot roll back an accepted operator submission.
            foreach (var readingId in committedSubmission.GeneratorReadings.Select(x => x.Id))
            {
                try { await fuelLoadSafety.EvaluateOnReceiptAsync(readingId, cancellationToken); }
                catch { /* the scheduled evaluator remains able to assess limits */ }
            }
            foreach (var readingId in committedSubmission.TankLevelReadings.Select(x => x.Id))
            {
                try { await fuelLoadSafety.EvaluateTankDeadStockAsync(readingId, cancellationToken); }
                catch { /* notification failures cannot roll back an accepted operator submission */ }
            }

            var accepted = ToDto(committedSubmission);
            var response = accepted with { Lines = accepted.Lines!.Concat(duplicateLines).ToList() };
            await idempotency.CompleteAsync(dto.ClientRequestId, 201, JsonSerializer.Serialize(response), userId, cancellationToken);
            return Result<OperationalSubmissionDto>.Success(response, 201);
        }
        catch (DbUpdateException)
        {
            // A filtered unique index is the final duplicate guard under concurrent writes.
            await idempotency.CompleteAsync(dto.ClientRequestId, 409, null, userId, cancellationToken);
            return Result<OperationalSubmissionDto>.Conflict("A reading for the same equipment and capture time already exists.");
        }
    }

    public async Task<IReadOnlyList<OperationalHistoryItemDto>> GetMyHistoryAsync(int userId, CancellationToken cancellationToken = default) =>
        await db.OperationalSubmissions.AsNoTracking().Where(x => x.CreatedBy == userId)
            .OrderByDescending(x => x.ReceivedAtUtc).Take(100)
            .Select(x => new OperationalHistoryItemDto(
                x.Id, x.SubStationId, x.SubStation.Name, x.ReceivedAtUtc, x.Status.ToString(),
                x.GeneratorReadings.Count, x.TankLevelReadings.Count, x.FeederLoadReadings.Count, x.MeterReadings.Count,
                x.TankLevelReadings
                    .OrderBy(t => t.CapturedAtUtc)
                    .Select(t => new OperationalTankHistoryLineDto(
                        t.Id, t.FuelTankId, t.FuelTank.Name, t.LiquidLevelCm, t.ComputedLitres,
                        t.Status.ToString(), t.EventType.ToString(), t.CapturedAtUtc))
                    .ToList()))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationalCaptureReferenceDto>> GetCaptureReferencesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var isCommercial = await _commercialMode.IsCommercialModeAsync(cancellationToken);
        int[]? siteIds = null;
        if (isCommercial)
        {
            var commercialScope = await _commercialMode.GetScopeAsync(cancellationToken);
            siteIds = commercialScope is null ? [] : [commercialScope.SubStationId];
        }
        else if (!currentUser.HasAnyAdminRole)
        {
            var accessible = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);
            siteIds = accessible.ToArray();
        }

        var sites = db.SubStations.AsNoTracking().Where(x => siteIds == null || siteIds.Contains(x.Id));
        return await sites.OrderBy(x => x.Name).Select(x => new OperationalCaptureReferenceDto(
            x.Id, x.Name, x.Code, db.StationOperationalSettings.Where(s => s.StationId == x.StationId).Select(s => s.TimeZone).FirstOrDefault() ?? StationOperationalSettings.DefaultTimeZone,
            x.Generators.OrderBy(g => g.Name).Select(g => new OperationalGeneratorReferenceDto(g.Id, g.Name, g.Model, db.Meters.Where(m => m.GeneratorId == g.Id).Select(m => (int?)m.Id).FirstOrDefault())).ToList(),
            x.FuelTanks.OrderBy(t => t.Name).Select(t => new OperationalTankReferenceDto(t.Id, t.Name, t.Code, t.Shape != null && t.DiameterCm != null && t.CylinderHeightCm != null && t.ConeHeightCm != null && t.MultiplicationFactor != null)).ToList(),
            x.Feeders.OrderBy(f => f.Name).Select(f => new OperationalFeederReferenceDto(f.Id, f.Name, f.Code, f.MeterMultiplier, f.MeterId)).ToList(),
            x.Meters.OrderBy(m => m.Name).Select(m => new OperationalMeterReferenceDto(m.Id, m.Name, m.SerialNumber, m.GeneratorId, m.MeterTypeLookup != null ? m.MeterTypeLookup.Code : null)).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result> VoidRecordedLineAsync(string lineType, int lineId, VoidOperationalReadingDto dto, int userId, CancellationToken cancellationToken = default)
    {
        if (lineId <= 0 || string.IsNullOrWhiteSpace(dto.Reason)) return Result.Failure("A void reason is required.");
        var now = DateTime.UtcNow;
        BaseEntity? entity = lineType.ToLowerInvariant() switch
        {
            "generator" => await db.OperationalGeneratorReadings.Include(x => x.OperationalSubmission).SingleOrDefaultAsync(x => x.Id == lineId, cancellationToken),
            "tank" => await db.TankLevelReadings.Include(x => x.OperationalSubmission).SingleOrDefaultAsync(x => x.Id == lineId, cancellationToken),
            "feeder" => await db.FeederLoadReadings.Include(x => x.OperationalSubmission).SingleOrDefaultAsync(x => x.Id == lineId, cancellationToken),
            _ => null,
        };
        if (entity is null) return Result.NotFound("Recorded reading was not found.");
        var siteId = entity switch
        {
            OperationalGeneratorReading x => x.OperationalSubmission.SubStationId,
            TankLevelReading x => x.OperationalSubmission.SubStationId,
            FeederLoadReading x => x.OperationalSubmission.SubStationId,
            _ => 0,
        };
        if (!await CanAccessSiteAsync(userId, siteId, cancellationToken)) return Result.NotFound("Recorded reading was not found.");
        var status = entity switch
        {
            OperationalGeneratorReading x => x.Status,
            TankLevelReading x => x.Status,
            FeederLoadReading x => x.Status,
            _ => OperationalReadingStatus.Pending,
        };
        if (status != OperationalReadingStatus.Recorded) return Result.Failure("Only recorded Tier-1 readings can be voided.", 409);
        switch (entity)
        {
            case OperationalGeneratorReading x: x.Status = OperationalReadingStatus.Voided; x.VoidReason = dto.Reason.Trim(); break;
            case TankLevelReading x: x.Status = OperationalReadingStatus.Voided; x.VoidReason = dto.Reason.Trim(); break;
            case FeederLoadReading x: x.Status = OperationalReadingStatus.Voided; x.VoidReason = dto.Reason.Trim(); break;
        }
        entity.DeletedAt = now; entity.UpdatedAt = now; entity.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<string?> ValidateAsync(SubmitOperationalCaptureDto dto, int userId, int siteId, DateTime now, CancellationToken cancellationToken)
    {
        var allTimes = dto.GeneratorReadings.Select(x => x.CapturedAtOverrideUtc ?? x.CapturedAtUtc).Concat(dto.TankLevelReadings.Select(x => x.CapturedAtOverrideUtc ?? x.CapturedAtUtc)).Concat(dto.FeederLoadReadings.Select(x => x.CapturedAtOverrideUtc ?? x.CapturedAtUtc)).Concat(dto.MeterReadings.Select(x => x.CapturedAtOverrideUtc ?? x.CapturedAtUtc));
        var deviceClockSkewed = dto.ClientSentAtUtc is DateTime clientSent && Math.Abs((now - NormalizeUtc(clientSent)).TotalMinutes) > FutureTolerance.TotalMinutes;
        if (!deviceClockSkewed && allTimes.Any(x => NormalizeUtc(x) > now.Add(FutureTolerance))) return "Captured time cannot be more than five minutes in the future.";
        if (dto.GeneratorReadings.Any(x => x.IsRunning && (x.LineVoltageVolts is null || x.CurrentAmps is null || x.WaterTemperatureCelsius is null || x.OilPressure is null || x.OilTemperatureCelsius is null || x.ActiveLoadKw is null)))
            return "Running generator lines require voltage, current, water temperature, oil pressure, oil temperature, and active load.";
        if (dto.GeneratorReadings.Any(x => x.PowerFactor is < 0 or > 1) || dto.GeneratorReadings.Any(x => x.OilAddedLitres < 0) || dto.FeederLoadReadings.Any(x => x.ActiveLoadKw < 0 || x.ApparentLoadKva < 0) || dto.MeterReadings.Any(x => x.RawValue < 0 || x.MultiplierSnapshot <= 0))
            return "Reading values are outside the permitted range.";
        if (dto.MeterReadings.Any(x => x.IsMeterReset && string.IsNullOrWhiteSpace(x.ResetNote))) return "A meter-reset note is required when reset is selected.";

        var allPhotoIds = dto.GeneratorReadings.SelectMany(x => x.PhotoAttachmentIds).Concat(dto.TankLevelReadings.SelectMany(x => x.PhotoAttachmentIds).Concat(dto.TankLevelReadings.SelectMany(x => x.BeforePhotoAttachmentIds ?? []))).Concat(dto.FeederLoadReadings.SelectMany(x => x.PhotoAttachmentIds)).Concat(dto.MeterReadings.SelectMany(x => x.PhotoAttachmentIds)).Distinct().ToList();
        if (!dto.IsOfflineQueue && (dto.GeneratorReadings.Any(x => x.PhotoAttachmentIds.Count == 0) || dto.TankLevelReadings.Any(x => x.PhotoAttachmentIds.Count == 0) || dto.FeederLoadReadings.Any(x => x.PhotoAttachmentIds.Count == 0) || dto.MeterReadings.Any(x => x.PhotoAttachmentIds.Count == 0)))
            return "Every reading line requires at least one photo.";
        if (dto.TankLevelReadings.Any(x => x.PhotoAttachmentIds.Count > 5 || (x.BeforePhotoAttachmentIds?.Count ?? 0) > 5))
            return "Tank level and event sides accept no more than five photos each.";
        if (dto.TankLevelReadings.Any(x => x.EventType is TankEventType.Receipt or TankEventType.TransferIn or TankEventType.TransferOut && (x.BeforeLiquidLevelCm is null || (x.BeforePhotoAttachmentIds?.Count ?? 0) == 0)))
            return "Tank receipt and transfer events require before level and before photo.";
        if (dto.TankLevelReadings.Any(x => x.EventType == TankEventType.Receipt && x.DeliveryNoteLitres is null))
            return "Tank receipt events require delivery-note litres.";
        var ownedAttachmentCount = await db.Attachments.CountAsync(x => allPhotoIds.Contains(x.Id) && x.CreatedBy == userId && (x.ContentType == "image/jpeg" || x.ContentType == "image/webp") && x.ByteSize > 0 && x.ByteSize <= StationOperationalSettings.DefaultReadingPhotoMaxBytes, cancellationToken);
        if (ownedAttachmentCount != allPhotoIds.Count) return "One or more photos are invalid or do not belong to the current operator.";

        var generatorIds = dto.GeneratorReadings.Select(x => x.GeneratorId).Distinct().ToList();
        if (await db.Generators.CountAsync(x => generatorIds.Contains(x.Id) && x.SubStationId == siteId, cancellationToken) != generatorIds.Count)
            return "Every generator must belong to the selected site.";
        var tankIds = dto.TankLevelReadings.Select(x => x.FuelTankId).Distinct().ToList();
        if (await db.FuelTanks.CountAsync(x => tankIds.Contains(x.Id) && x.SubStationId == siteId, cancellationToken) != tankIds.Count)
            return "Every tank must belong to the selected site.";
        var tanksById = await db.FuelTanks.AsNoTracking().Where(x => tankIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        foreach (var line in dto.TankLevelReadings)
        {
            if (line.EventType == TankEventType.Receipt && (tanksById[line.FuelTankId].ParentTankId is not null || tanksById[line.FuelTankId].TankRole == TankRole.Daily))
                return "Tank receipts must be recorded on a main tank.";
            if (line.EventType is TankEventType.TransferOut or TankEventType.TransferIn)
            {
                if (line.LinkedClientLineId is null || line.ClientLineId is null) return "Linked transfer lines require device line IDs.";
                var paired = dto.TankLevelReadings.SingleOrDefault(x => x.ClientLineId == line.LinkedClientLineId);
                if (paired is null || (line.EventType == TankEventType.TransferOut && paired.EventType != TankEventType.TransferIn) || (line.EventType == TankEventType.TransferIn && paired.EventType != TankEventType.TransferOut))
                    return "Each tank transfer must contain linked outgoing and incoming lines.";
                if (line.EventType == TankEventType.TransferIn)
                {
                    var dest = tanksById[line.FuelTankId];
                    var source = tanksById[paired.FuelTankId];
                    var destIsDaily = dest.TankRole == TankRole.Daily || (dest.TankRole != TankRole.Main && dest.ParentTankId is not null);
                    var sourceIsMain = source.TankRole == TankRole.Main || (source.TankRole != TankRole.Daily && source.ParentTankId is null);
                    if (!destIsDaily || !sourceIsMain || dest.ParentTankId != paired.FuelTankId)
                        return "The incoming transfer tank must be a daily tank of the outgoing main tank.";
                }


            }
        }
        var feederIds = dto.FeederLoadReadings.Select(x => x.FeederId).Distinct().ToList();
        if (await db.Feeders.CountAsync(x => feederIds.Contains(x.Id) && x.SubStationId == siteId, cancellationToken) != feederIds.Count)
            return "Every feeder must belong to the selected site.";
        if (tanksById.Values.Any(x => !TankLevelConverter.IsConfigured(x)))
            return "Each selected tank must be calibrated before a level capture is submitted.";
        foreach (var line in dto.TankLevelReadings)
        {
            var tank = tanksById[line.FuelTankId];
            var maxLevelCm = TankLevelConverter.MaxLevelCm(tank);
            if (line.LiquidLevelCm < 0m || (maxLevelCm.HasValue && line.LiquidLevelCm > maxLevelCm.Value))
            {
                return maxLevelCm.HasValue
                    ? $"Tank level for '{tank.Name}' must be between 0 and {maxLevelCm.Value} cm."
                    : $"Tank level for '{tank.Name}' must be 0 cm or greater.";
            }
            if (line.BeforeLiquidLevelCm.HasValue &&
                (line.BeforeLiquidLevelCm.Value < 0m || (maxLevelCm.HasValue && line.BeforeLiquidLevelCm.Value > maxLevelCm.Value)))
            {
                return maxLevelCm.HasValue
                    ? $"Previous tank level for '{tank.Name}' must be between 0 and {maxLevelCm.Value} cm."
                    : $"Previous tank level for '{tank.Name}' must be 0 cm or greater.";
            }
        }

        var dailyTankCoverageError = await ValidateCommercialDailyTankCoverageAsync(
            dto,
            await _commercialMode.IsCommercialModeAsync(cancellationToken),
            cancellationToken);
        if (dailyTankCoverageError is not null)
            return dailyTankCoverageError;

        if (dto.GeneratorReadings.GroupBy(x => new { x.GeneratorId, At = NormalizeUtc(x.CapturedAtOverrideUtc ?? x.CapturedAtUtc) }).Any(x => x.Count() > 1) || dto.TankLevelReadings.GroupBy(x => new { x.FuelTankId, At = NormalizeUtc(x.CapturedAtOverrideUtc ?? x.CapturedAtUtc) }).Any(x => x.Count() > 1) || dto.FeederLoadReadings.GroupBy(x => new { x.FeederId, At = NormalizeUtc(x.CapturedAtOverrideUtc ?? x.CapturedAtUtc) }).Any(x => x.Count() > 1) || dto.MeterReadings.GroupBy(x => new { x.SubjectType, x.SubjectId, At = NormalizeUtc(x.CapturedAtOverrideUtc ?? x.CapturedAtUtc) }).Any(x => x.Count() > 1))
            return "A subject may appear only once at the same capture time.";

        foreach (var line in dto.GeneratorReadings)
            if (await db.OperationalGeneratorReadings.AnyAsync(x => x.GeneratorId == line.GeneratorId && x.CapturedAtUtc == NormalizeUtc(line.CapturedAtOverrideUtc ?? line.CapturedAtUtc) && x.Status != OperationalReadingStatus.Rejected && x.Status != OperationalReadingStatus.Voided, cancellationToken))
                return "A reading for the same equipment and capture time already exists.";
        foreach (var line in dto.TankLevelReadings)
            if (await db.TankLevelReadings.AnyAsync(x => x.FuelTankId == line.FuelTankId && x.CapturedAtUtc == NormalizeUtc(line.CapturedAtOverrideUtc ?? line.CapturedAtUtc) && x.Status != OperationalReadingStatus.Rejected && x.Status != OperationalReadingStatus.Voided, cancellationToken))
                return "A reading for the same equipment and capture time already exists.";
        foreach (var line in dto.FeederLoadReadings)
            if (await db.FeederLoadReadings.AnyAsync(x => x.FeederId == line.FeederId && x.CapturedAtUtc == NormalizeUtc(line.CapturedAtOverrideUtc ?? line.CapturedAtUtc) && x.Status != OperationalReadingStatus.Rejected && x.Status != OperationalReadingStatus.Voided, cancellationToken))
                return "A reading for the same equipment and capture time already exists.";
        foreach (var line in dto.MeterReadings)
            if (await db.MeterReadings.AnyAsync(x => x.SubjectType == line.SubjectType && x.SubjectId == line.SubjectId && x.CapturedAtUtc == NormalizeUtc(line.CapturedAtOverrideUtc ?? line.CapturedAtUtc) && x.Status != OperationalReadingStatus.Rejected && x.Status != OperationalReadingStatus.Voided, cancellationToken))
                return "A reading for the same equipment and capture time already exists.";

        var meterIds = dto.MeterReadings.Where(x => x.MeterId.HasValue).Select(x => x.MeterId!.Value).Distinct().ToList();
        if (meterIds.Count > 0 && await db.Meters.CountAsync(x => meterIds.Contains(x.Id) && x.SubStationId == siteId, cancellationToken) != meterIds.Count)
            return "Every selected meter must belong to the selected site.";
        var meterGeneratorIds = dto.MeterReadings.Where(x => x.SubjectType is MeterReadingSubjectType.GeneratorEnergyProduced or MeterReadingSubjectType.GeneratorRunningHours).Select(x => x.SubjectId).Distinct().ToList();
        var meterFeederIds = dto.MeterReadings.Where(x => x.SubjectType == MeterReadingSubjectType.FeederEnergySent).Select(x => x.SubjectId).Distinct().ToList();
        if (await db.Generators.CountAsync(x => meterGeneratorIds.Contains(x.Id) && x.SubStationId == siteId, cancellationToken) != meterGeneratorIds.Count ||
            await db.Feeders.CountAsync(x => meterFeederIds.Contains(x.Id) && x.SubStationId == siteId, cancellationToken) != meterFeederIds.Count)
            return "Every meter-reading subject must belong to the selected site.";
        return null;
    }

    private async Task<string?> ValidateCommercialDailyTankCoverageAsync(
        SubmitOperationalCaptureDto dto,
        bool isCommercial,
        CancellationToken cancellationToken)
    {
        if (!isCommercial || !dto.MeterReadings.Any(x => x.Kind == MeterReadingKind.DailyClosing))
            return null;

        var dailyGeneratorIds = dto.GeneratorReadings.Select(x => x.GeneratorId)
            .Concat(dto.MeterReadings
                .Where(x => x.SubjectType is MeterReadingSubjectType.GeneratorEnergyProduced or MeterReadingSubjectType.GeneratorRunningHours)
                .Select(x => x.SubjectId))
            .Distinct()
            .ToList();
        if (dailyGeneratorIds.Count == 0)
            return null;

        var covered = await db.FuelTanks.AsNoTracking()
            .Where(x => x.IsActive && x.DeletedAt == null && x.TankRole == TankRole.Daily &&
                        x.GeneratorId.HasValue && dailyGeneratorIds.Contains(x.GeneratorId.Value))
            .Select(x => x.GeneratorId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var missing = dailyGeneratorIds.Except(covered).ToList();
        if (missing.Count == 0)
            return null;

        var names = await db.Generators.AsNoTracking()
            .Where(x => missing.Contains(x.Id))
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);
        return $"GENERATOR_NO_DAILY_TANK: {string.Join(", ", names)}";
    }

    private async Task<(SubmitOperationalCaptureDto Dto, IReadOnlyList<OperationalLineResultDto> Duplicates)> ExcludeKnownClientLinesAsync(SubmitOperationalCaptureDto dto, CancellationToken cancellationToken)
    {
        var ids = dto.GeneratorReadings.Select(x => x.ClientLineId).Concat(dto.TankLevelReadings.Select(x => x.ClientLineId)).Concat(dto.FeederLoadReadings.Select(x => x.ClientLineId)).Concat(dto.MeterReadings.Select(x => x.ClientLineId))
            .Where(x => x.HasValue && x.Value != Guid.Empty).Select(x => x.Value).Distinct().ToList();
        if (ids.Count == 0) return (dto, []);
        var existing = new Dictionary<Guid, int>();
        foreach (var row in await db.OperationalGeneratorReadings.AsNoTracking().Where(x => ids.Contains(x.ClientLineId)).Select(x => new { x.ClientLineId, x.Id }).ToListAsync(cancellationToken)) existing[row.ClientLineId] = row.Id;
        foreach (var row in await db.TankLevelReadings.AsNoTracking().Where(x => ids.Contains(x.ClientLineId)).Select(x => new { x.ClientLineId, x.Id }).ToListAsync(cancellationToken)) existing[row.ClientLineId] = row.Id;
        foreach (var row in await db.FeederLoadReadings.AsNoTracking().Where(x => ids.Contains(x.ClientLineId)).Select(x => new { x.ClientLineId, x.Id }).ToListAsync(cancellationToken)) existing[row.ClientLineId] = row.Id;
        foreach (var row in await db.MeterReadings.AsNoTracking().Where(x => ids.Contains(x.ClientLineId)).Select(x => new { x.ClientLineId, x.Id }).ToListAsync(cancellationToken)) existing[row.ClientLineId] = row.Id;
        if (existing.Count == 0) return (dto, []);
        var duplicates = existing.Select(x => new OperationalLineResultDto(x.Key, "duplicate", null, x.Value)).ToList();
        return (dto with
        {
            GeneratorReadings = dto.GeneratorReadings.Where(x => x.ClientLineId is null || !existing.ContainsKey(x.ClientLineId.Value)).ToList(),
            TankLevelReadings = dto.TankLevelReadings.Where(x => x.ClientLineId is null || !existing.ContainsKey(x.ClientLineId.Value)).ToList(),
            FeederLoadReadings = dto.FeederLoadReadings.Where(x => x.ClientLineId is null || !existing.ContainsKey(x.ClientLineId.Value)).ToList(),
            MeterReadings = dto.MeterReadings.Where(x => x.ClientLineId is null || !existing.ContainsKey(x.ClientLineId.Value)).ToList(),
        }, duplicates);
    }

    /// <summary>
    /// Lines are independent in an hourly round. Validate each independent line (or both sides of a
    /// tank transfer) before the final batch validation so an invalid line does not discard valid work.
    /// </summary>
    private async Task<(SubmitOperationalCaptureDto Dto, IReadOnlyList<OperationalLineResultDto> Rejections)> ExcludeRejectedLinesAsync(
        SubmitOperationalCaptureDto dto, int userId, int siteId, DateTime now, CancellationToken cancellationToken)
    {
        var rejected = new Dictionary<Guid, string>();

        async Task ValidateCandidateAsync(SubmitOperationalCaptureDto candidate, IEnumerable<Guid?> ids)
        {
            var error = await ValidateAsync(candidate, userId, siteId, now, cancellationToken);
            if (error is null) return;
            foreach (var id in ids.Where(x => x.HasValue && x.Value != Guid.Empty).Select(x => x.Value))
                rejected.TryAdd(id, error);
        }

        foreach (var line in dto.GeneratorReadings)
            await ValidateCandidateAsync(dto with { GeneratorReadings = [line], TankLevelReadings = [], FeederLoadReadings = [], MeterReadings = [] }, [line.ClientLineId]);
        foreach (var line in dto.FeederLoadReadings)
            await ValidateCandidateAsync(dto with { GeneratorReadings = [], TankLevelReadings = [], FeederLoadReadings = [line], MeterReadings = [] }, [line.ClientLineId]);
        foreach (var line in dto.MeterReadings)
            await ValidateCandidateAsync(dto with { GeneratorReadings = [], TankLevelReadings = [], FeederLoadReadings = [], MeterReadings = [line] }, [line.ClientLineId]);

        var processedTransferLines = new HashSet<Guid>();
        foreach (var line in dto.TankLevelReadings)
        {
            if (line.EventType is not (TankEventType.TransferOut or TankEventType.TransferIn))
            {
                await ValidateCandidateAsync(dto with { GeneratorReadings = [], TankLevelReadings = [line], FeederLoadReadings = [], MeterReadings = [] }, [line.ClientLineId]);
                continue;
            }

            var lineId = line.ClientLineId!.Value;
            if (!processedTransferLines.Add(lineId)) continue;
            var paired = line.LinkedClientLineId is Guid linkedId
                ? dto.TankLevelReadings.SingleOrDefault(x => x.ClientLineId == linkedId)
                : null;
            var pair = paired is null ? new[] { line } : new[] { line, paired };
            if (paired?.ClientLineId is Guid pairedId) processedTransferLines.Add(pairedId);
            await ValidateCandidateAsync(dto with { GeneratorReadings = [], TankLevelReadings = pair, FeederLoadReadings = [], MeterReadings = [] }, pair.Select(x => x.ClientLineId));
        }

        if (rejected.Count == 0) return (dto, []);
        var results = rejected.Select(x => new OperationalLineResultDto(x.Key, "rejected", x.Value)).ToList();
        return (dto with
        {
            GeneratorReadings = dto.GeneratorReadings.Where(x => !rejected.ContainsKey(x.ClientLineId!.Value)).ToList(),
            TankLevelReadings = dto.TankLevelReadings.Where(x => !rejected.ContainsKey(x.ClientLineId!.Value)).ToList(),
            FeederLoadReadings = dto.FeederLoadReadings.Where(x => !rejected.ContainsKey(x.ClientLineId!.Value)).ToList(),
            MeterReadings = dto.MeterReadings.Where(x => !rejected.ContainsKey(x.ClientLineId!.Value)).ToList(),
        }, results);
    }

    private async Task<(SubmitOperationalCaptureDto Dto, IReadOnlyList<OperationalLineResultDto> Duplicates)> ExcludeDuplicateTimesAsync(
        SubmitOperationalCaptureDto dto, DateTime receivedAtUtc, CancellationToken cancellationToken)
    {
        var duplicateIds = new HashSet<Guid>();
        var seenGeneratorSlots = new HashSet<(int GeneratorId, DateTime CapturedAtUtc)>();
        var seenTankSlots = new HashSet<(int FuelTankId, DateTime CapturedAtUtc)>();
        var seenFeederSlots = new HashSet<(int FeederId, DateTime CapturedAtUtc)>();
        var seenMeterSlots = new HashSet<(MeterReadingSubjectType SubjectType, int SubjectId, DateTime CapturedAtUtc)>();

        foreach (var line in dto.GeneratorReadings.Where(x => x.ClientLineId is { } id && id != Guid.Empty))
        {
            var captured = ResolveCapturedAt(line.CapturedAtOverrideUtc ?? line.CapturedAtUtc, dto.SubmissionCapturedAtUtc, receivedAtUtc);
            var key = (line.GeneratorId, captured);
            if (!seenGeneratorSlots.Add(key) || await db.OperationalGeneratorReadings.AsNoTracking().AnyAsync(x => x.GeneratorId == line.GeneratorId && x.CapturedAtUtc == captured && x.Status != OperationalReadingStatus.Rejected && x.Status != OperationalReadingStatus.Voided, cancellationToken))
                duplicateIds.Add(line.ClientLineId!.Value);
        }
        foreach (var line in dto.TankLevelReadings.Where(x => x.ClientLineId is { } id && id != Guid.Empty))
        {
            var captured = ResolveCapturedAt(line.CapturedAtOverrideUtc ?? line.CapturedAtUtc, dto.SubmissionCapturedAtUtc, receivedAtUtc);
            var key = (line.FuelTankId, captured);
            if (!seenTankSlots.Add(key) || await db.TankLevelReadings.AsNoTracking().AnyAsync(x => x.FuelTankId == line.FuelTankId && x.CapturedAtUtc == captured && x.Status != OperationalReadingStatus.Rejected && x.Status != OperationalReadingStatus.Voided, cancellationToken))
                duplicateIds.Add(line.ClientLineId!.Value);
        }
        foreach (var line in dto.FeederLoadReadings.Where(x => x.ClientLineId is { } id && id != Guid.Empty))
        {
            var captured = ResolveCapturedAt(line.CapturedAtOverrideUtc ?? line.CapturedAtUtc, dto.SubmissionCapturedAtUtc, receivedAtUtc);
            var key = (line.FeederId, captured);
            if (!seenFeederSlots.Add(key) || await db.FeederLoadReadings.AsNoTracking().AnyAsync(x => x.FeederId == line.FeederId && x.CapturedAtUtc == captured && x.Status != OperationalReadingStatus.Rejected && x.Status != OperationalReadingStatus.Voided, cancellationToken))
                duplicateIds.Add(line.ClientLineId!.Value);
        }
        foreach (var line in dto.MeterReadings.Where(x => x.ClientLineId is { } id && id != Guid.Empty))
        {
            var captured = ResolveCapturedAt(line.CapturedAtOverrideUtc ?? line.CapturedAtUtc, dto.SubmissionCapturedAtUtc, receivedAtUtc);
            var key = (line.SubjectType, line.SubjectId, captured);
            if (!seenMeterSlots.Add(key) || await db.MeterReadings.AsNoTracking().AnyAsync(x => x.SubjectType == line.SubjectType && x.SubjectId == line.SubjectId && x.CapturedAtUtc == captured && x.Status != OperationalReadingStatus.Rejected && x.Status != OperationalReadingStatus.Voided, cancellationToken))
                duplicateIds.Add(line.ClientLineId!.Value);
        }

        if (duplicateIds.Count == 0) return (dto, []);
        var duplicates = duplicateIds.Select(id => new OperationalLineResultDto(id, "duplicate_time", "A reading already exists for this subject and minute.")).ToList();
        return (dto with
        {
            GeneratorReadings = dto.GeneratorReadings.Where(x => x.ClientLineId is null || !duplicateIds.Contains(x.ClientLineId.Value)).ToList(),
            TankLevelReadings = dto.TankLevelReadings.Where(x => x.ClientLineId is null || !duplicateIds.Contains(x.ClientLineId.Value)).ToList(),
            FeederLoadReadings = dto.FeederLoadReadings.Where(x => x.ClientLineId is null || !duplicateIds.Contains(x.ClientLineId.Value)).ToList(),
            MeterReadings = dto.MeterReadings.Where(x => x.ClientLineId is null || !duplicateIds.Contains(x.ClientLineId.Value)).ToList(),
        }, duplicates);
    }

    private async Task<bool> CanAccessSiteAsync(int userId, int siteId, CancellationToken cancellationToken) =>
        currentUser.HasAnyAdminRole || await scopeFilter.HasAccessToSubStationAsync(userId, siteId);

    private async Task<TimeZoneInfo> GetStationTimeZoneAsync(int stationId, CancellationToken cancellationToken)
    {
        var id = await db.StationOperationalSettings.AsNoTracking().Where(x => x.StationId == stationId).Select(x => x.TimeZone).SingleOrDefaultAsync(cancellationToken) ?? StationOperationalSettings.DefaultTimeZone;
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        var utc = value.Kind switch { DateTimeKind.Utc => value, DateTimeKind.Local => value.ToUniversalTime(), _ => DateTime.SpecifyKind(value, DateTimeKind.Utc) };
        return new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, 0, DateTimeKind.Utc);
    }
    private static DateTime ResolveCapturedAt(DateTime lineTime, DateTime? submissionTime, DateTime now) =>
        NormalizeUtc(lineTime == default ? submissionTime ?? now : lineTime);
    private static DateOnly BusinessDate(DateTime utc, TimeZoneInfo zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, zone));
    private static string? WarningJson(DateTime captured, DateTime received, DateTime? clientSent, bool reusedPhoto, bool atDeadStock = false)
    {
        var warnings = new List<string>();
        if (received - captured > SkewWarning) warnings.Add("capture_received_skew");
        if (clientSent is DateTime sent && Math.Abs((received - NormalizeUtc(sent)).TotalHours) > SkewWarning.TotalHours) warnings.Add("client_clock_skew");
        if (reusedPhoto) warnings.Add("photo_reused_other_submission");
        if (atDeadStock) warnings.Add("operational.warnings.dailyTankAtDeadStock");
        return warnings.Count == 0 ? null : JsonSerializer.Serialize(warnings);
    }
    private static string Hash(SubmitOperationalCaptureDto dto) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto)))).ToLowerInvariant();
    private static OperationalSubmissionDto ToDto(OperationalSubmission entity)
    {
        var lines = entity.GeneratorReadings.Select(x => new OperationalLineResultDto(x.ClientLineId, x.Status == OperationalReadingStatus.AwaitingPhoto ? "accepted" : "accepted", null, x.Id))
            .Concat(entity.TankLevelReadings.Select(x => new OperationalLineResultDto(x.ClientLineId, "accepted", null, x.Id)))
            .Concat(entity.FeederLoadReadings.Select(x => new OperationalLineResultDto(x.ClientLineId, "accepted", null, x.Id)))
            .Concat(entity.MeterReadings.Select(x => new OperationalLineResultDto(x.ClientLineId, "accepted", null, x.Id)))
            .ToList();
        return new OperationalSubmissionDto(entity.Id, entity.SubStationId, entity.ReceivedAtUtc, entity.Status.ToString(), entity.GeneratorReadings.Count, entity.TankLevelReadings.Count, entity.FeederLoadReadings.Count, entity.MeterReadings.Count, lines);
    }
}
