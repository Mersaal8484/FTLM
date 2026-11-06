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

/// <summary>Owns the Phase 2 atomic raw-capture command. It never produces official values.</summary>
public sealed class OperationalCaptureService(
    FuelTrackDbContext db,
    IUserScopeFilterService scopeFilter,
    ICurrentUserService currentUser,
    IIdempotencyService idempotency) : IOperationalCaptureService
{
    private const string Operation = "operational.capture.v1";
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SkewWarning = TimeSpan.FromHours(12);

    public async Task<Result<OperationalSubmissionDto>> SubmitAsync(SubmitOperationalCaptureDto dto, int userId, CancellationToken cancellationToken = default)
    {
        if (dto.ClientRequestId == Guid.Empty || dto.SubStationId <= 0)
            return Result<OperationalSubmissionDto>.Failure("A site and client request ID are required.");
        var lineCount = dto.GeneratorReadings.Count + dto.TankLevelReadings.Count + dto.FeederLoadReadings.Count + dto.MeterReadings.Count;
        if (lineCount == 0) return Result<OperationalSubmissionDto>.Failure("At least one reading line is required.");
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
            var timeZone = await GetStationTimeZoneAsync(site.StationId, cancellationToken);
            var validation = await ValidateAsync(dto, userId, site.Id, now, cancellationToken);
            if (validation is not null)
            {
                await idempotency.CompleteAsync(dto.ClientRequestId, 400, null, userId, cancellationToken);
                return Result<OperationalSubmissionDto>.Failure(validation);
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var submission = new OperationalSubmission
            {
                SubStationId = dto.SubStationId,
                ClientRequestId = dto.ClientRequestId,
                ReceivedAtUtc = now,
                Status = OperationalReadingStatus.Pending,
                CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId,
            };
            db.OperationalSubmissions.Add(submission);

            var attachmentLinks = new List<(int AttachmentId, object Entity)>();
            foreach (var line in dto.GeneratorReadings)
            {
                var captured = NormalizeUtc(line.CapturedAtUtc);
                var entity = new OperationalGeneratorReading
                {
                    OperationalSubmission = submission, GeneratorId = line.GeneratorId, CapturedAtUtc = captured, ReceivedAtUtc = now,
                    BusinessDate = BusinessDate(captured, timeZone), Status = OperationalReadingStatus.Pending, IsRunning = line.IsRunning,
                    LineVoltageVolts = line.LineVoltageVolts, CurrentAmps = line.CurrentAmps, WaterTemperatureCelsius = line.WaterTemperatureCelsius,
                    OilPressure = line.OilPressure, OilTemperatureCelsius = line.OilTemperatureCelsius, ActiveLoadKw = line.ActiveLoadKw,
                    ApparentLoadKva = line.ApparentLoadKva, ReactiveLoadKvar = line.ReactiveLoadKvar, FrequencyHz = line.FrequencyHz,
                    OilAddedLitres = line.OilAddedLitres, PowerFactor = line.PowerFactor,
                    WarningsJson = WarningJson(captured, now), CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId,
                };
                db.OperationalGeneratorReadings.Add(entity);
                attachmentLinks.AddRange(line.PhotoAttachmentIds.Select(id => (id, (object)entity)));
            }

            var tankCalibration = await db.FuelTanks.Where(x => dto.TankLevelReadings.Select(y => y.FuelTankId).Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);
            foreach (var line in dto.TankLevelReadings)
            {
                var tank = tankCalibration[line.FuelTankId];
                var captured = NormalizeUtc(line.CapturedAtUtc);
                var litres = TankGeometry.CalculateLitres(tank.DiameterCm!.Value, tank.CylinderHeightCm!.Value, tank.ConeHeightCm!.Value, tank.MultiplicationFactor!.Value, line.LiquidLevelCm);
                var entity = new TankLevelReading
                {
                    OperationalSubmission = submission, FuelTankId = tank.Id, CapturedAtUtc = captured, ReceivedAtUtc = now,
                    BusinessDate = BusinessDate(captured, timeZone), Status = OperationalReadingStatus.Pending, LiquidLevelCm = line.LiquidLevelCm,
                    ComputedLitres = litres, ShapeSnapshot = tank.Shape!.Value, DiameterCmSnapshot = tank.DiameterCm.Value,
                    CylinderHeightCmSnapshot = tank.CylinderHeightCm.Value, ConeHeightCmSnapshot = tank.ConeHeightCm.Value,
                    MultiplicationFactorSnapshot = tank.MultiplicationFactor.Value, WarningsJson = WarningJson(captured, now),
                    CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId,
                };
                db.TankLevelReadings.Add(entity);
                attachmentLinks.AddRange(line.PhotoAttachmentIds.Select(id => (id, (object)entity)));
            }

            var feederMap = await db.Feeders.Where(x => dto.FeederLoadReadings.Select(y => y.FeederId).Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);
            foreach (var line in dto.FeederLoadReadings)
            {
                var feeder = feederMap[line.FeederId];
                var captured = NormalizeUtc(line.CapturedAtUtc);
                var entity = new FeederLoadReading
                {
                    OperationalSubmission = submission, FeederId = feeder.Id, CapturedAtUtc = captured, ReceivedAtUtc = now,
                    BusinessDate = BusinessDate(captured, timeZone), Status = OperationalReadingStatus.Pending,
                    ActiveLoadKw = line.ActiveLoadKw, ApparentLoadKva = line.ApparentLoadKva, MeterMultiplierSnapshot = feeder.MeterMultiplier,
                    WarningsJson = WarningJson(captured, now), CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId,
                };
                db.FeederLoadReadings.Add(entity);
                attachmentLinks.AddRange(line.PhotoAttachmentIds.Select(id => (id, (object)entity)));
            }

            foreach (var line in dto.MeterReadings)
            {
                var captured = NormalizeUtc(line.CapturedAtUtc);
                var multiplier = line.MultiplierSnapshot ?? 1m;
                var entity = new MeterReading
                {
                    OperationalSubmission = submission, SubjectType = line.SubjectType, SubjectId = line.SubjectId, MeterId = line.MeterId,
                    Kind = line.Kind, CapturedAtUtc = captured, ReceivedAtUtc = now, BusinessDate = BusinessDate(captured, timeZone),
                    Status = OperationalReadingStatus.Pending, RawValue = line.RawValue, MultiplierSnapshot = multiplier,
                    IsMeterReset = line.IsMeterReset, ResetNote = line.ResetNote?.Trim(), WarningsJson = WarningJson(captured, now),
                    CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId,
                };
                db.MeterReadings.Add(entity);
                attachmentLinks.AddRange(line.PhotoAttachmentIds.Select(id => (id, (object)entity)));
            }

            await db.SaveChangesAsync(cancellationToken);
            foreach (var (attachmentId, entity) in attachmentLinks)
                db.AttachmentLinks.Add(new AttachmentLink
                {
                    AttachmentId = attachmentId, EntityType = entity.GetType().Name, EntityId = ((BaseEntity)entity).Id,
                    SortOrder = 0, CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId,
                });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var response = ToDto(submission);
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
            .Select(x => new OperationalHistoryItemDto(x.Id, x.SubStationId, x.SubStation.Name, x.ReceivedAtUtc, x.Status.ToString(),
                x.GeneratorReadings.Count, x.TankLevelReadings.Count, x.FeederLoadReadings.Count, x.MeterReadings.Count))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationalCaptureReferenceDto>> GetCaptureReferencesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var siteIds = currentUser.HasAnyAdminRole ? null : await scopeFilter.GetAccessibleSubStationIdsAsync(userId);
        var sites = db.SubStations.AsNoTracking().Where(x => siteIds == null || siteIds.Contains(x.Id));
        return await sites.OrderBy(x => x.Name).Select(x => new OperationalCaptureReferenceDto(
            x.Id, x.Name, x.Code,
            x.Generators.OrderBy(g => g.Name).Select(g => new OperationalGeneratorReferenceDto(g.Id, g.Name, g.Model, db.Meters.Where(m => m.GeneratorId == g.Id).Select(m => (int?)m.Id).FirstOrDefault())).ToList(),
            x.FuelTanks.OrderBy(t => t.Name).Select(t => new OperationalTankReferenceDto(t.Id, t.Name, t.Code, t.Shape != null && t.DiameterCm != null && t.CylinderHeightCm != null && t.ConeHeightCm != null && t.MultiplicationFactor != null)).ToList(),
            x.Feeders.OrderBy(f => f.Name).Select(f => new OperationalFeederReferenceDto(f.Id, f.Name, f.Code, f.MeterMultiplier, f.MeterId)).ToList(),
            x.Meters.OrderBy(m => m.Name).Select(m => new OperationalMeterReferenceDto(m.Id, m.Name, m.SerialNumber, m.GeneratorId)).ToList()))
            .ToListAsync(cancellationToken);
    }

    private async Task<string?> ValidateAsync(SubmitOperationalCaptureDto dto, int userId, int siteId, DateTime now, CancellationToken cancellationToken)
    {
        var allTimes = dto.GeneratorReadings.Select(x => x.CapturedAtUtc).Concat(dto.TankLevelReadings.Select(x => x.CapturedAtUtc)).Concat(dto.FeederLoadReadings.Select(x => x.CapturedAtUtc)).Concat(dto.MeterReadings.Select(x => x.CapturedAtUtc));
        if (allTimes.Any(x => NormalizeUtc(x) > now.Add(FutureTolerance))) return "Captured time cannot be more than five minutes in the future.";
        if (dto.GeneratorReadings.Any(x => x.IsRunning && (x.LineVoltageVolts is null || x.CurrentAmps is null || x.WaterTemperatureCelsius is null || x.OilPressure is null || x.OilTemperatureCelsius is null || x.ActiveLoadKw is null)))
            return "Running generator lines require voltage, current, water temperature, oil pressure, oil temperature, and active load.";
        if (dto.GeneratorReadings.Any(x => x.PowerFactor is < 0 or > 1) || dto.GeneratorReadings.Any(x => x.OilAddedLitres < 0) || dto.FeederLoadReadings.Any(x => x.ActiveLoadKw < 0 || x.ApparentLoadKva < 0) || dto.MeterReadings.Any(x => x.RawValue < 0 || x.MultiplierSnapshot <= 0))
            return "Reading values are outside the permitted range.";
        if (dto.MeterReadings.Any(x => x.IsMeterReset && string.IsNullOrWhiteSpace(x.ResetNote))) return "A meter-reset note is required when reset is selected.";

        var allPhotoIds = dto.GeneratorReadings.SelectMany(x => x.PhotoAttachmentIds).Concat(dto.TankLevelReadings.SelectMany(x => x.PhotoAttachmentIds)).Concat(dto.FeederLoadReadings.SelectMany(x => x.PhotoAttachmentIds)).Concat(dto.MeterReadings.SelectMany(x => x.PhotoAttachmentIds)).Distinct().ToList();
        if (dto.GeneratorReadings.Any(x => x.PhotoAttachmentIds.Count == 0) || dto.TankLevelReadings.Any(x => x.PhotoAttachmentIds.Count == 0) || dto.FeederLoadReadings.Any(x => x.PhotoAttachmentIds.Count == 0) || dto.MeterReadings.Any(x => x.PhotoAttachmentIds.Count == 0))
            return "Every reading line requires at least one photo.";
        var ownedAttachmentCount = await db.Attachments.CountAsync(x => allPhotoIds.Contains(x.Id) && x.CreatedBy == userId && (x.ContentType == "image/jpeg" || x.ContentType == "image/webp") && x.ByteSize > 0 && x.ByteSize <= StationOperationalSettings.DefaultReadingPhotoMaxBytes, cancellationToken);
        if (ownedAttachmentCount != allPhotoIds.Count) return "One or more photos are invalid or do not belong to the current operator.";

        var generatorIds = dto.GeneratorReadings.Select(x => x.GeneratorId).Distinct().ToList();
        if (await db.Generators.CountAsync(x => generatorIds.Contains(x.Id) && x.SubStationId == siteId, cancellationToken) != generatorIds.Count)
            return "Every generator must belong to the selected site.";
        var tankIds = dto.TankLevelReadings.Select(x => x.FuelTankId).Distinct().ToList();
        if (await db.FuelTanks.CountAsync(x => tankIds.Contains(x.Id) && x.SubStationId == siteId, cancellationToken) != tankIds.Count)
            return "Every tank must belong to the selected site.";
        var feederIds = dto.FeederLoadReadings.Select(x => x.FeederId).Distinct().ToList();
        if (await db.Feeders.CountAsync(x => feederIds.Contains(x.Id) && x.SubStationId == siteId, cancellationToken) != feederIds.Count)
            return "Every feeder must belong to the selected site.";
        if (await db.FuelTanks.AnyAsync(x => tankIds.Contains(x.Id) && (x.Shape == null || x.DiameterCm == null || x.CylinderHeightCm == null || x.ConeHeightCm == null || x.MultiplicationFactor == null), cancellationToken))
            return "Each selected tank must be calibrated before a level capture is submitted.";

        if (dto.GeneratorReadings.GroupBy(x => new { x.GeneratorId, At = NormalizeUtc(x.CapturedAtUtc) }).Any(x => x.Count() > 1) || dto.TankLevelReadings.GroupBy(x => new { x.FuelTankId, At = NormalizeUtc(x.CapturedAtUtc) }).Any(x => x.Count() > 1) || dto.FeederLoadReadings.GroupBy(x => new { x.FeederId, At = NormalizeUtc(x.CapturedAtUtc) }).Any(x => x.Count() > 1) || dto.MeterReadings.GroupBy(x => new { x.SubjectType, x.SubjectId, At = NormalizeUtc(x.CapturedAtUtc) }).Any(x => x.Count() > 1))
            return "A subject may appear only once at the same capture time.";

        foreach (var line in dto.GeneratorReadings)
            if (await db.OperationalGeneratorReadings.AnyAsync(x => x.GeneratorId == line.GeneratorId && x.CapturedAtUtc == NormalizeUtc(line.CapturedAtUtc) && x.Status != OperationalReadingStatus.Rejected, cancellationToken))
                return "A reading for the same equipment and capture time already exists.";
        foreach (var line in dto.TankLevelReadings)
            if (await db.TankLevelReadings.AnyAsync(x => x.FuelTankId == line.FuelTankId && x.CapturedAtUtc == NormalizeUtc(line.CapturedAtUtc) && x.Status != OperationalReadingStatus.Rejected, cancellationToken))
                return "A reading for the same equipment and capture time already exists.";
        foreach (var line in dto.FeederLoadReadings)
            if (await db.FeederLoadReadings.AnyAsync(x => x.FeederId == line.FeederId && x.CapturedAtUtc == NormalizeUtc(line.CapturedAtUtc) && x.Status != OperationalReadingStatus.Rejected, cancellationToken))
                return "A reading for the same equipment and capture time already exists.";
        foreach (var line in dto.MeterReadings)
            if (await db.MeterReadings.AnyAsync(x => x.SubjectType == line.SubjectType && x.SubjectId == line.SubjectId && x.CapturedAtUtc == NormalizeUtc(line.CapturedAtUtc) && x.Status != OperationalReadingStatus.Rejected, cancellationToken))
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

    private async Task<bool> CanAccessSiteAsync(int userId, int siteId, CancellationToken cancellationToken) =>
        currentUser.HasAnyAdminRole || await scopeFilter.HasAccessToSubStationAsync(userId, siteId);

    private async Task<TimeZoneInfo> GetStationTimeZoneAsync(int stationId, CancellationToken cancellationToken)
    {
        var id = await db.StationOperationalSettings.AsNoTracking().Where(x => x.StationId == stationId).Select(x => x.TimeZone).SingleOrDefaultAsync(cancellationToken) ?? StationOperationalSettings.DefaultTimeZone;
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch { DateTimeKind.Utc => value, DateTimeKind.Local => value.ToUniversalTime(), _ => DateTime.SpecifyKind(value, DateTimeKind.Utc) };
    private static DateOnly BusinessDate(DateTime utc, TimeZoneInfo zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, zone));
    private static string? WarningJson(DateTime captured, DateTime received) => received - captured > SkewWarning ? "[\"capture_received_skew\"]" : null;
    private static string Hash(SubmitOperationalCaptureDto dto) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto)))).ToLowerInvariant();
    private static OperationalSubmissionDto ToDto(OperationalSubmission entity) => new(entity.Id, entity.SubStationId, entity.ReceivedAtUtc, entity.Status.ToString(), entity.GeneratorReadings.Count, entity.TankLevelReadings.Count, entity.FeederLoadReadings.Count, entity.MeterReadings.Count);
}
