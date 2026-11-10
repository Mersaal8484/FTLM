using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FuelTrack.Infrastructure.Services;

public sealed class ServiceReplacementService : IServiceReplacementService
{
    private readonly FuelTrackDbContext _db;
    private readonly IServiceDueEvaluationService _dueEvaluation;
    private readonly ILogger<ServiceReplacementService> _logger;

    public ServiceReplacementService(
        FuelTrackDbContext db,
        IServiceDueEvaluationService dueEvaluation,
        ILogger<ServiceReplacementService> logger)
    {
        _db = db;
        _dueEvaluation = dueEvaluation;
        _logger = logger;
    }

    public async Task<Result<ServiceReplacementRecordDto>> RecordReplacementAsync(
        RecordReplacementRequestDto dto,
        int userId,
        CancellationToken ct = default)
    {
        var generator = await _db.Generators
            .Include(g => g.SubStation)
            .FirstOrDefaultAsync(g => g.Id == dto.GeneratorId, ct);

        if (generator is null)
        {
            return Result<ServiceReplacementRecordDto>.NotFound($"Generator with ID {dto.GeneratorId} not found.");
        }

        var serviceItem = await _db.ServiceItems
            .FirstOrDefaultAsync(i => i.Id == dto.ServiceItemId, ct);

        if (serviceItem is null)
        {
            return Result<ServiceReplacementRecordDto>.NotFound($"Service item with ID {dto.ServiceItemId} not found.");
        }

        var modelKey = GeneratorModelKey.Normalize(generator.Model);
        var definition = await _db.ServiceIntervalDefinitions
            .FirstOrDefaultAsync(d => d.GeneratorModelKey == modelKey && d.ServiceItemId == dto.ServiceItemId, ct);

        var intervalHours = definition?.IntervalHours ?? serviceItem.SuggestedIntervalHours ?? 250m;

        // Find previous active record
        var previousRecord = await _db.ServiceReplacementRecords
            .Where(r => r.GeneratorId == dto.GeneratorId && r.ServiceItemId == dto.ServiceItemId && r.VoidedAtUtc == null)
            .OrderByDescending(r => r.HoursAtService)
            .ThenByDescending(r => r.OccurredAtUtc)
            .FirstOrDefaultAsync(ct);

        var isMeterReset = false;
        if (previousRecord is not null)
        {
            isMeterReset = await _db.MeterReadings.AnyAsync(m =>
                m.SubjectType == MeterReadingSubjectType.GeneratorRunningHours &&
                m.SubjectId == dto.GeneratorId &&
                m.IsMeterReset &&
                ReadingApprovalPolicy.OfficialStatuses.Contains(m.Status) &&
                m.CapturedAtUtc > previousRecord.OccurredAtUtc, ct);
        }

        var validation = ServiceDueCalculator.ValidateReplacementHours(
            dto.HoursAtService,
            previousRecord?.HoursAtService,
            intervalHours,
            ServiceRecordKind.Replacement,
            dto.EarlyReplacementReason,
            isMeterReset);

        if (!validation.IsValid)
        {
            return Result<ServiceReplacementRecordDto>.Failure(validation.ErrorMessage ?? "Validation failed.");
        }

        var record = new ServiceReplacementRecord
        {
            GeneratorId = dto.GeneratorId,
            ServiceItemId = dto.ServiceItemId,
            Kind = ServiceRecordKind.Replacement,
            HoursAtService = dto.HoursAtService,
            OccurredAtUtc = dto.OccurredAtUtc,
            WorkPermitId = dto.WorkPermitId,
            IsEarly = validation.IsEarly,
            EarlyReplacementReason = validation.IsEarly ? dto.EarlyReplacementReason?.Trim() : null,
            IntervalHoursSnapshot = intervalHours,
            Notes = dto.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _db.ServiceReplacementRecords.Add(record);
        await _db.SaveChangesAsync(ct);

        // Immediate recalculation of due state
        try
        {
            await _dueEvaluation.EvaluateGeneratorAsync(dto.GeneratorId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate due state after recording replacement for generator {GeneratorId}", dto.GeneratorId);
        }

        return Result<ServiceReplacementRecordDto>.Success(new ServiceReplacementRecordDto(
            record.Id,
            generator.Id,
            generator.Name,
            serviceItem.Id,
            serviceItem.Code,
            serviceItem.NameAr,
            serviceItem.NameEn,
            record.Kind,
            record.HoursAtService,
            record.OccurredAtUtc,
            record.WorkPermitId,
            record.IsEarly,
            record.EarlyReplacementReason,
            record.IntervalHoursSnapshot,
            record.Notes,
            record.VoidedAtUtc,
            record.VoidReason,
            record.CreatedBy,
            record.CreatedAt), 201);
    }

    public async Task<Result<IReadOnlyList<ServiceReplacementRecordDto>>> RecordBulkBaselineAsync(
        BulkBaselineRequestDto dto,
        int userId,
        CancellationToken ct = default)
    {
        var generator = await _db.Generators
            .Include(g => g.SubStation)
            .FirstOrDefaultAsync(g => g.Id == dto.GeneratorId, ct);

        if (generator is null)
        {
            return Result<IReadOnlyList<ServiceReplacementRecordDto>>.NotFound($"Generator with ID {dto.GeneratorId} not found.");
        }

        var modelKey = GeneratorModelKey.Normalize(generator.Model);
        var definitions = await _db.ServiceIntervalDefinitions
            .Where(d => d.GeneratorModelKey == modelKey)
            .ToDictionaryAsync(d => d.ServiceItemId, ct);

        var itemIds = dto.Items.Select(x => x.ServiceItemId).Distinct().ToList();
        var serviceItems = await _db.ServiceItems
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        var createdRecords = new List<ServiceReplacementRecord>();

        foreach (var row in dto.Items)
        {
            if (!serviceItems.TryGetValue(row.ServiceItemId, out var sItem))
            {
                return Result<IReadOnlyList<ServiceReplacementRecordDto>>.NotFound($"Service item ID {row.ServiceItemId} not found.");
            }

            definitions.TryGetValue(row.ServiceItemId, out var def);
            var interval = def?.IntervalHours ?? sItem.SuggestedIntervalHours ?? 250m;

            var record = new ServiceReplacementRecord
            {
                GeneratorId = dto.GeneratorId,
                ServiceItemId = row.ServiceItemId,
                Kind = ServiceRecordKind.Baseline,
                HoursAtService = row.HoursAtService,
                OccurredAtUtc = row.OccurredAtUtc,
                IntervalHoursSnapshot = interval,
                Notes = row.Notes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            };

            _db.ServiceReplacementRecords.Add(record);
            createdRecords.Add(record);
        }

        await _db.SaveChangesAsync(ct);

        try
        {
            await _dueEvaluation.EvaluateGeneratorAsync(dto.GeneratorId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate due state after bulk baseline for generator {GeneratorId}", dto.GeneratorId);
        }

        var resultList = createdRecords.Select(r =>
        {
            var sItem = serviceItems[r.ServiceItemId];
            return new ServiceReplacementRecordDto(
                r.Id,
                generator.Id,
                generator.Name,
                sItem.Id,
                sItem.Code,
                sItem.NameAr,
                sItem.NameEn,
                r.Kind,
                r.HoursAtService,
                r.OccurredAtUtc,
                r.WorkPermitId,
                r.IsEarly,
                r.EarlyReplacementReason,
                r.IntervalHoursSnapshot,
                r.Notes,
                r.VoidedAtUtc,
                r.VoidReason,
                r.CreatedBy,
                r.CreatedAt);
        }).ToList();

        return Result<IReadOnlyList<ServiceReplacementRecordDto>>.Success(resultList, 201);
    }

    public async Task<Result<bool>> VoidAsync(
        int id,
        VoidReplacementRecordDto dto,
        int userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return Result<bool>.Failure("Void reason is mandatory.");
        }

        var record = await _db.ServiceReplacementRecords
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (record is null)
        {
            return Result<bool>.NotFound($"Service replacement record with ID {id} not found.");
        }

        if (record.VoidedAtUtc.HasValue)
        {
            return Result<bool>.Failure("This service replacement record has already been voided.");
        }

        record.VoidedAtUtc = DateTime.UtcNow;
        record.VoidedByUserId = userId;
        record.VoidReason = dto.Reason.Trim();
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);

        try
        {
            await _dueEvaluation.EvaluateGeneratorAsync(record.GeneratorId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate due state after voiding record {RecordId} for generator {GeneratorId}", id, record.GeneratorId);
        }

        return Result<bool>.Success(true);
    }

    public async Task<IReadOnlyList<ServiceReplacementRecordDto>> GetHistoryAsync(
        int generatorId,
        int? serviceItemId = null,
        CancellationToken ct = default)
    {
        var query = _db.ServiceReplacementRecords
            .AsNoTracking()
            .Include(r => r.Generator)
            .Include(r => r.ServiceItem)
            .Where(r => r.GeneratorId == generatorId);

        if (serviceItemId.HasValue)
        {
            query = query.Where(r => r.ServiceItemId == serviceItemId.Value);
        }

        var records = await query
            .OrderByDescending(r => r.OccurredAtUtc)
            .ThenByDescending(r => r.Id)
            .ToListAsync(ct);

        return records.Select(r => new ServiceReplacementRecordDto(
            r.Id,
            r.GeneratorId,
            r.Generator.Name,
            r.ServiceItemId,
            r.ServiceItem.Code,
            r.ServiceItem.NameAr,
            r.ServiceItem.NameEn,
            r.Kind,
            r.HoursAtService,
            r.OccurredAtUtc,
            r.WorkPermitId,
            r.IsEarly,
            r.EarlyReplacementReason,
            r.IntervalHoursSnapshot,
            r.Notes,
            r.VoidedAtUtc,
            r.VoidReason,
            r.CreatedBy,
            r.CreatedAt)).ToList();
    }
}
