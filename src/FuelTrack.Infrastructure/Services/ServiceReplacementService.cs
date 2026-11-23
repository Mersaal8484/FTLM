using FuelTrack.Application.Common;
using FuelTrack.Application.Constants;
using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FuelTrack.Infrastructure.Services;

public sealed class ServiceReplacementService : IServiceReplacementService
{
    private readonly FuelTrackDbContext _db;
    private readonly IServiceDueEvaluationService _dueEvaluation;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdempotencyService _idempotency;
    private readonly ILogger<ServiceReplacementService> _logger;
    private readonly ICommercialStationModeService? _commercialMode;
    private readonly IOilLedgerService? _oilLedger;

    public ServiceReplacementService(
        FuelTrackDbContext db,
        IServiceDueEvaluationService dueEvaluation,
        IUserScopeFilterService scopeFilter,
        ICurrentUserService currentUser,
        IIdempotencyService idempotency,
        ILogger<ServiceReplacementService> logger,
        ICommercialStationModeService? commercialMode = null,
        IOilLedgerService? oilLedger = null)
    {
        _db = db;
        _dueEvaluation = dueEvaluation;
        _scopeFilter = scopeFilter;
        _currentUser = currentUser;
        _idempotency = idempotency;
        _logger = logger;
        _commercialMode = commercialMode;
        _oilLedger = oilLedger;
    }

    public async Task<Result<ServiceReplacementRecordDto>> RecordReplacementAsync(
        RecordReplacementRequestDto dto,
        int userId,
        CancellationToken ct = default)
    {
        var scopedGenerator = await GetScopedGeneratorAsync(dto.GeneratorId, ct);
        if (!scopedGenerator.IsSuccess)
            return Result<ServiceReplacementRecordDto>.Failure(scopedGenerator.Error!, scopedGenerator.StatusCode);
        var generator = scopedGenerator.Value!;

        var serviceItem = await _db.ServiceItems
            .FirstOrDefaultAsync(i => i.Id == dto.ServiceItemId, ct);

        if (serviceItem is null)
            return Result<ServiceReplacementRecordDto>.NotFound($"Service item with ID {dto.ServiceItemId} not found.");

        if (!serviceItem.IsActive)
            return Result<ServiceReplacementRecordDto>.Failure("Cannot record replacement for an inactive service item.", 400);

        var modelKey = GeneratorModelKey.Normalize(generator.Model);
        var definition = await _db.ServiceIntervalDefinitions
            .FirstOrDefaultAsync(d => d.GeneratorModelKey == modelKey && d.ServiceItemId == dto.ServiceItemId, ct);

        // Recording a replacement/baseline for an item WITHOUT a definition is ALLOWED (P4-9, C4).
        // It is never early and stores IntervalHoursSnapshot = null.
        decimal? intervalHours = definition?.IntervalHours;

        // Find previous active record by OccurredAtUtc then Id (W3)
        var previousRecord = await ServiceRecordHelper.GetLatestActiveRecordAsync(_db, dto.GeneratorId, dto.ServiceItemId, ct);
        var isMeterReset = await ServiceRecordHelper.HasMeterResetAfterAsync(_db, dto.GeneratorId, previousRecord?.OccurredAtUtc, ct);

        var isEarly = intervalHours.HasValue &&
                      previousRecord is not null &&
                      !isMeterReset &&
                      (dto.HoursAtService - previousRecord.HoursAtService) < intervalHours.Value;

        var validation = ServiceDueCalculator.ValidateReplacementHours(
            dto.HoursAtService,
            previousRecord?.HoursAtService,
            intervalHours ?? 0m,
            ServiceRecordKind.Replacement,
            dto.EarlyReplacementReason,
            isMeterReset);

        // When definition is null, intervalHours is null so isEarly is false and no early reason is required
        if (intervalHours is null)
        {
            if (dto.HoursAtService < 0)
                return Result<ServiceReplacementRecordDto>.Failure("Hours at service must be greater than or equal to 0.", 400);

            if (previousRecord is not null && !isMeterReset && dto.HoursAtService < previousRecord.HoursAtService)
                return Result<ServiceReplacementRecordDto>.Failure("Hours at service cannot be less than previous service hours without a meter reset.", 400);
        }
        else if (!validation.IsValid)
        {
            // Fail pre-validation directly without registering idempotency key (C3)
            return Result<ServiceReplacementRecordDto>.Failure(validation.ErrorMessage ?? "Validation failed.", 400);
        }

        var isCommercial = _commercialMode is not null && await _commercialMode.IsCommercialModeAsync(ct);
        GeneratorMaintenanceOilConfiguration? oilConfig = null;
        var warnings = new List<string>();

        if (isCommercial)
        {
            oilConfig = await _db.GeneratorMaintenanceOilConfigurations
                .Include(x => x.Oil)
                .FirstOrDefaultAsync(x => x.GeneratorId == generator.Id && x.ServiceItemId == serviceItem.Id, ct);

            if (oilConfig is null)
            {
                warnings.Add(ServiceReplacementWarningCodes.OilNotConfigured);
            }
            else
            {
                if (oilConfig.QuantityLiters <= 0)
                    return Result<ServiceReplacementRecordDto>.Failure("The configured maintenance oil quantity must be greater than zero.", 400);

                if (oilConfig.Oil.SubStationId != generator.SubStationId)
                    return Result<ServiceReplacementRecordDto>.Failure("The configured maintenance oil is invalid for this generator.", 400);

                var currentStock = _oilLedger is not null
                    ? await _oilLedger.GetCurrentBalanceAsync(generator.SubStationId, oilConfig.Oil.OilLookupId, ct)
                    : oilConfig.Oil.CurrentStock;

                if (currentStock < oilConfig.QuantityLiters)
                {
                    warnings.Add(ServiceReplacementWarningCodes.OilStockInsufficient);
                }
            }
        }

        int? consumptionTypeId = null;
        if (oilConfig is not null)
        {
            consumptionTypeId = await _db.Lookups
                .Where(x => x.Code == OilTransactionTypeCodes.Consumption && (x.Category.Name == "OilTransactionType" || x.CategoryId == 100) && x.IsActive)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(ct) ?? 206;
        }

        // Only register idempotency command once validation has completely passed (C3)
        var idempotency = await StartIdempotentCommandAsync(dto.ClientRequestId, "service-replacement", dto, userId, ct);
        if (!idempotency.IsSuccess)
            return Result<ServiceReplacementRecordDto>.Failure(idempotency.Error!, idempotency.StatusCode);
        if (!idempotency.Value!.IsNew)
            return Replay<ServiceReplacementRecordDto>(idempotency.Value);

        (ServiceReplacementRecord Record, OilTransaction? OilTransaction) created;
        try
        {
            created = await DbTransactionRunner.RunAsync(_db, async trCt =>
            {
                var record = new ServiceReplacementRecord
                {
                    GeneratorId = dto.GeneratorId,
                    ServiceItemId = dto.ServiceItemId,
                    Kind = ServiceRecordKind.Replacement,
                    HoursAtService = dto.HoursAtService,
                    OccurredAtUtc = dto.OccurredAtUtc,
                    WorkPermitId = dto.WorkPermitId,
                    IsEarly = isEarly,
                    EarlyReplacementReason = isEarly ? dto.EarlyReplacementReason?.Trim() : null,
                    IntervalHoursSnapshot = intervalHours,
                    Notes = dto.Notes?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = userId
                };

                _db.ServiceReplacementRecords.Add(record);
                await _db.SaveChangesAsync(trCt);

                OilTransaction? createdOilTx = null;
                if (oilConfig is not null)
                {
                    var now = DateTime.UtcNow;
                    createdOilTx = new OilTransaction
                    {
                        SubStationId = generator.SubStationId,
                        OilLookupId = oilConfig.Oil.OilLookupId,
                        TransactionTypeLookupId = consumptionTypeId!.Value,
                        TransactionDate = record.OccurredAtUtc,
                        QuantityLiters = (double)oilConfig.QuantityLiters,
                        Reference = $"SRV-{record.Id}",
                        Notes = $"Maintenance replacement: {serviceItem.NameAr ?? serviceItem.NameEn} on {generator.Name}",
                        IsApproved = true,
                        ApprovedById = userId,
                        ApprovedAt = now,
                        CorrectedFromId = null,
                        CreatedAt = now,
                        CreatedBy = userId,
                        UpdatedAt = now,
                        UpdatedBy = userId
                    };

                    _db.OilTransactions.Add(createdOilTx);
                    await _db.SaveChangesAsync(trCt);

                    record.OilTransactionId = createdOilTx.Id;
                    await _db.SaveChangesAsync(trCt);
                }

                return (record, createdOilTx);
            }, ct);
        }
        catch
        {
            await _idempotency.ReleaseAsync(dto.ClientRequestId, userId, ct);
            throw;
        }

        var record = created.Record;

        try
        {
            await _dueEvaluation.EvaluateGeneratorAsync(dto.GeneratorId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate due state after recording replacement for generator {GeneratorId}", dto.GeneratorId);
        }

        var response = new ServiceReplacementRecordDto(
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
            record.CreatedAt,
            record.OilTransactionId,
            warnings.Count > 0 ? warnings : null);

        if (dto.ClientRequestId != Guid.Empty)
            await _idempotency.CompleteAsync(dto.ClientRequestId, 201, JsonSerializer.Serialize(response), userId, ct);

        return Result<ServiceReplacementRecordDto>.Success(response, 201);
    }

    public async Task<Result<IReadOnlyList<ServiceReplacementRecordDto>>> RecordBulkBaselineAsync(
        BulkBaselineRequestDto dto,
        int userId,
        CancellationToken ct = default)
    {
        var detailed = await RecordBulkBaselineDetailedAsync(dto, userId, ct);
        if (!detailed.IsSuccess)
            return Result<IReadOnlyList<ServiceReplacementRecordDto>>.Failure(detailed.Error!, detailed.StatusCode);

        var createdIds = detailed.Value!.RowResults.Where(r => r.Success && r.RecordId.HasValue).Select(r => r.RecordId!.Value).ToList();
        var records = await _db.ServiceReplacementRecords
            .AsNoTracking()
            .Include(r => r.Generator)
            .Include(r => r.ServiceItem)
            .Where(r => createdIds.Contains(r.Id))
            .ToListAsync(ct);

        var dtos = records.Select(r => new ServiceReplacementRecordDto(
            r.Id,
            r.GeneratorId,
            r.Generator?.Name ?? string.Empty,
            r.ServiceItemId,
            r.ServiceItem?.Code ?? string.Empty,
            r.ServiceItem?.NameAr ?? string.Empty,
            r.ServiceItem?.NameEn ?? string.Empty,
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
            r.CreatedAt,
            r.OilTransactionId)).ToList();

        return Result<IReadOnlyList<ServiceReplacementRecordDto>>.Success(dtos, 201);
    }

    public async Task<Result<BulkBaselineResultDto>> RecordBulkBaselineDetailedAsync(
        BulkBaselineRequestDto dto,
        int userId,
        CancellationToken ct = default)
    {
        var scopedGenerator = await GetScopedGeneratorAsync(dto.GeneratorId, ct);
        if (!scopedGenerator.IsSuccess)
            return Result<BulkBaselineResultDto>.Failure(scopedGenerator.Error!, scopedGenerator.StatusCode);
        var generator = scopedGenerator.Value!;

        if (dto.Items is null || dto.Items.Count == 0)
            return Result<BulkBaselineResultDto>.Failure("At least one baseline item must be provided.", 400);

        var modelKey = GeneratorModelKey.Normalize(generator.Model);
        var definitions = await _db.ServiceIntervalDefinitions
            .AsNoTracking()
            .Where(d => d.GeneratorModelKey == modelKey)
            .ToDictionaryAsync(d => d.ServiceItemId, ct);

        var itemIds = dto.Items.Select(x => x.ServiceItemId).Distinct().ToList();
        var serviceItems = await _db.ServiceItems
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        // Pre-validate rows and separate valid rows from invalid rows (W5)
        var rowResults = new List<BulkBaselineRowResultDto>();
        var validRowsToInsert = new List<(BulkBaselineRowDto row, decimal? intervalHours)>();

        foreach (var row in dto.Items)
        {
            if (!serviceItems.TryGetValue(row.ServiceItemId, out var sItem))
            {
                rowResults.Add(new BulkBaselineRowResultDto(row.ServiceItemId, null, false, null, $"Service item ID {row.ServiceItemId} not found."));
                continue;
            }

            if (!sItem.IsActive)
            {
                rowResults.Add(new BulkBaselineRowResultDto(row.ServiceItemId, sItem.Code, false, null, "Cannot baseline an inactive service item."));
                continue;
            }

            if (row.HoursAtService < 0)
            {
                rowResults.Add(new BulkBaselineRowResultDto(row.ServiceItemId, sItem.Code, false, null, "Hours at service must be >= 0."));
                continue;
            }

            // Check previous record monotonicity
            var prevRecord = await ServiceRecordHelper.GetLatestActiveRecordAsync(_db, dto.GeneratorId, row.ServiceItemId, ct);
            var isReset = await ServiceRecordHelper.HasMeterResetAfterAsync(_db, dto.GeneratorId, prevRecord?.OccurredAtUtc, ct);

            if (prevRecord is not null && !isReset && row.HoursAtService < prevRecord.HoursAtService)
            {
                rowResults.Add(new BulkBaselineRowResultDto(row.ServiceItemId, sItem.Code, false, null, "Baseline hours cannot regress without a meter reset."));
                continue;
            }

            definitions.TryGetValue(row.ServiceItemId, out var def);
            decimal? interval = def?.IntervalHours; // Null allowed if not configured (C4)
            validRowsToInsert.Add((row, interval));
        }

        if (validRowsToInsert.Count == 0)
        {
            // All rows failed validation
            return Result<BulkBaselineResultDto>.Failure("All rows failed validation.", 400);
        }

        // Idempotency check for the valid batch
        var idempotency = await StartIdempotentCommandAsync(dto.ClientRequestId, "service-baseline", dto, userId, ct);
        if (!idempotency.IsSuccess)
            return Result<BulkBaselineResultDto>.Failure(idempotency.Error!, idempotency.StatusCode);
        if (!idempotency.Value!.IsNew)
            return Replay<BulkBaselineResultDto>(idempotency.Value);

        var createdRecords = new List<ServiceReplacementRecord>();
        foreach (var (row, interval) in validRowsToInsert)
        {
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

        foreach (var rec in createdRecords)
        {
            var itemCode = serviceItems.TryGetValue(rec.ServiceItemId, out var si) ? si.Code : null;
            rowResults.Add(new BulkBaselineRowResultDto(rec.ServiceItemId, itemCode, true, rec.Id, null));
        }

        try
        {
            await _dueEvaluation.EvaluateGeneratorAsync(dto.GeneratorId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate due state after bulk baseline for generator {GeneratorId}", dto.GeneratorId);
        }

        var resultDto = new BulkBaselineResultDto(
            dto.GeneratorId,
            dto.Items.Count,
            createdRecords.Count,
            dto.Items.Count - createdRecords.Count,
            rowResults);

        if (dto.ClientRequestId != Guid.Empty)
            await _idempotency.CompleteAsync(dto.ClientRequestId, 201, JsonSerializer.Serialize(resultDto), userId, ct);

        return Result<BulkBaselineResultDto>.Success(resultDto, 201);
    }

    public async Task<Result<IReadOnlyList<ServiceReplacementRecordDto>>> RecordMultiItemReplacementAsync(
        MultiItemReplacementRequestDto dto,
        int userId,
        CancellationToken ct = default)
    {
        var scopedGenerator = await GetScopedGeneratorAsync(dto.GeneratorId, ct);
        if (!scopedGenerator.IsSuccess)
            return Result<IReadOnlyList<ServiceReplacementRecordDto>>.Failure(scopedGenerator.Error!, scopedGenerator.StatusCode);
        var generator = scopedGenerator.Value!;

        if (dto.Items is null || dto.Items.Count == 0)
            return Result<IReadOnlyList<ServiceReplacementRecordDto>>.Failure("At least one service item must be provided.", 400);

        if (dto.HoursAtService < 0)
            return Result<IReadOnlyList<ServiceReplacementRecordDto>>.Failure("Hours at service must be >= 0.", 400);

        var modelKey = GeneratorModelKey.Normalize(generator.Model);
        var definitions = await _db.ServiceIntervalDefinitions
            .AsNoTracking()
            .Where(d => d.GeneratorModelKey == modelKey)
            .ToDictionaryAsync(d => d.ServiceItemId, ct);

        var itemIds = dto.Items.Select(x => x.ServiceItemId).Distinct().ToList();
        var serviceItems = await _db.ServiceItems
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        var isCommercial = _commercialMode is not null && await _commercialMode.IsCommercialModeAsync(ct);
        var configs = isCommercial
            ? await _db.GeneratorMaintenanceOilConfigurations
                .Include(x => x.Oil)
                .Where(x => x.GeneratorId == generator.Id)
                .ToListAsync(ct)
            : new List<GeneratorMaintenanceOilConfiguration>();
        var configByItem = configs.ToDictionary(x => x.ServiceItemId);

        int? consumptionTypeId = null;
        if (configs.Count > 0)
        {
            consumptionTypeId = await _db.Lookups
                .Where(x => x.Code == OilTransactionTypeCodes.Consumption && (x.Category.Name == "OilTransactionType" || x.CategoryId == 100) && x.IsActive)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(ct) ?? 206;
        }

        var simulatedBalances = new Dictionary<int, decimal>();
        var itemPlans = new List<(MultiItemReplacementRowDto Row, ServiceItem Item, decimal? Interval, bool IsEarly, string? EarlyReason, GeneratorMaintenanceOilConfiguration? OilConfig, List<string> Warnings)>();

        foreach (var itemRow in dto.Items)
        {
            if (!serviceItems.TryGetValue(itemRow.ServiceItemId, out var sItem))
                return Result<IReadOnlyList<ServiceReplacementRecordDto>>.NotFound($"Service item ID {itemRow.ServiceItemId} not found.");

            if (!sItem.IsActive)
                return Result<IReadOnlyList<ServiceReplacementRecordDto>>.Failure($"Service item {sItem.Code} is inactive.", 400);

            definitions.TryGetValue(itemRow.ServiceItemId, out var def);
            decimal? interval = def?.IntervalHours;

            var previousRecord = await ServiceRecordHelper.GetLatestActiveRecordAsync(_db, dto.GeneratorId, itemRow.ServiceItemId, ct);
            var isMeterReset = await ServiceRecordHelper.HasMeterResetAfterAsync(_db, dto.GeneratorId, previousRecord?.OccurredAtUtc, ct);

            if (previousRecord is not null && !isMeterReset && dto.HoursAtService < previousRecord.HoursAtService)
                return Result<IReadOnlyList<ServiceReplacementRecordDto>>.Failure($"Hours at service cannot regress for item {sItem.Code}.", 400);

            var isEarly = interval.HasValue &&
                          previousRecord is not null &&
                          !isMeterReset &&
                          (dto.HoursAtService - previousRecord.HoursAtService) < interval.Value;

            if (isEarly && string.IsNullOrWhiteSpace(itemRow.EarlyReplacementReason))
                return Result<IReadOnlyList<ServiceReplacementRecordDto>>.Failure($"Early replacement reason is required for item {sItem.Code}.", 400);

            var itemWarnings = new List<string>();
            GeneratorMaintenanceOilConfiguration? itemOilConfig = null;

            if (isCommercial)
            {
                if (configByItem.TryGetValue(itemRow.ServiceItemId, out itemOilConfig))
                {
                    if (itemOilConfig.QuantityLiters <= 0)
                        return Result<IReadOnlyList<ServiceReplacementRecordDto>>.Failure("The configured maintenance oil quantity must be greater than zero.", 400);
                    if (itemOilConfig.Oil.SubStationId != generator.SubStationId)
                        return Result<IReadOnlyList<ServiceReplacementRecordDto>>.Failure("The configured maintenance oil is invalid for this generator.", 400);

                    var oilLookupId = itemOilConfig.Oil.OilLookupId;
                    if (!simulatedBalances.TryGetValue(oilLookupId, out var balance))
                    {
                        balance = _oilLedger is not null
                            ? await _oilLedger.GetCurrentBalanceAsync(generator.SubStationId, oilLookupId, ct)
                            : itemOilConfig.Oil.CurrentStock;
                    }

                    if (balance < itemOilConfig.QuantityLiters)
                    {
                        itemWarnings.Add(ServiceReplacementWarningCodes.OilStockInsufficient);
                    }
                    simulatedBalances[oilLookupId] = balance - itemOilConfig.QuantityLiters;
                }
                else
                {
                    itemWarnings.Add(ServiceReplacementWarningCodes.OilNotConfigured);
                }
            }

            itemPlans.Add((itemRow, sItem, interval, isEarly, isEarly ? itemRow.EarlyReplacementReason?.Trim() : null, itemOilConfig, itemWarnings));
        }

        var idempotency = await StartIdempotentCommandAsync(dto.ClientRequestId, "service-multi-replacement", dto, userId, ct);
        if (!idempotency.IsSuccess)
            return Result<IReadOnlyList<ServiceReplacementRecordDto>>.Failure(idempotency.Error!, idempotency.StatusCode);
        if (!idempotency.Value!.IsNew)
            return Replay<IReadOnlyList<ServiceReplacementRecordDto>>(idempotency.Value);

        List<(ServiceReplacementRecord Record, ServiceItem Item, GeneratorMaintenanceOilConfiguration? OilConfig, List<string> Warnings)> createdRecords;
        try
        {
            createdRecords = await DbTransactionRunner.RunAsync(_db, async trCt =>
            {
                var attemptRecords = new List<(ServiceReplacementRecord Record, ServiceItem Item, GeneratorMaintenanceOilConfiguration? OilConfig, List<string> Warnings)>();
                foreach (var plan in itemPlans)
                {
                    var record = new ServiceReplacementRecord
                    {
                        GeneratorId = dto.GeneratorId,
                        ServiceItemId = plan.Item.Id,
                        Kind = ServiceRecordKind.Replacement,
                        HoursAtService = dto.HoursAtService,
                        OccurredAtUtc = dto.OccurredAtUtc,
                        WorkPermitId = dto.WorkPermitId,
                        IsEarly = plan.IsEarly,
                        EarlyReplacementReason = plan.EarlyReason,
                        IntervalHoursSnapshot = plan.Interval,
                        Notes = plan.Row.Notes?.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = userId
                    };
                    _db.ServiceReplacementRecords.Add(record);
                    attemptRecords.Add((record, plan.Item, plan.OilConfig, plan.Warnings));
                }
                await _db.SaveChangesAsync(trCt);

                var now = DateTime.UtcNow;
                foreach (var item in attemptRecords)
                {
                    if (item.OilConfig is not null)
                    {
                        var oilTx = new OilTransaction
                        {
                            SubStationId = generator.SubStationId,
                            OilLookupId = item.OilConfig.Oil.OilLookupId,
                            TransactionTypeLookupId = consumptionTypeId!.Value,
                            TransactionDate = item.Record.OccurredAtUtc,
                            QuantityLiters = (double)item.OilConfig.QuantityLiters,
                            Reference = $"SRV-{item.Record.Id}",
                            Notes = $"Maintenance replacement: {item.Item.NameAr ?? item.Item.NameEn} on {generator.Name}",
                            IsApproved = true,
                            ApprovedById = userId,
                            ApprovedAt = now,
                            CorrectedFromId = null,
                            CreatedAt = now,
                            CreatedBy = userId,
                            UpdatedAt = now,
                            UpdatedBy = userId
                        };
                        _db.OilTransactions.Add(oilTx);
                        await _db.SaveChangesAsync(trCt);

                        item.Record.OilTransactionId = oilTx.Id;
                        await _db.SaveChangesAsync(trCt);
                    }
                }

                return attemptRecords;
            }, ct);
        }
        catch
        {
            await _idempotency.ReleaseAsync(dto.ClientRequestId, userId, ct);
            throw;
        }

        try
        {
            await _dueEvaluation.EvaluateGeneratorAsync(dto.GeneratorId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate due state after multi replacement for generator {GeneratorId}", dto.GeneratorId);
        }

        var dtos = createdRecords.Select(r => new ServiceReplacementRecordDto(
            r.Record.Id,
            r.Record.GeneratorId,
            generator.Name,
            r.Item.Id,
            r.Item.Code,
            r.Item.NameAr,
            r.Item.NameEn,
            r.Record.Kind,
            r.Record.HoursAtService,
            r.Record.OccurredAtUtc,
            r.Record.WorkPermitId,
            r.Record.IsEarly,
            r.Record.EarlyReplacementReason,
            r.Record.IntervalHoursSnapshot,
            r.Record.Notes,
            r.Record.VoidedAtUtc,
            r.Record.VoidReason,
            r.Record.CreatedBy,
            r.Record.CreatedAt,
            r.Record.OilTransactionId,
            r.Warnings.Count > 0 ? r.Warnings : null)).ToList();

        if (dto.ClientRequestId != Guid.Empty)
            await _idempotency.CompleteAsync(dto.ClientRequestId, 201, JsonSerializer.Serialize(dtos), userId, ct);

        return Result<IReadOnlyList<ServiceReplacementRecordDto>>.Success(dtos, 201);
    }

    public async Task<Result<bool>> VoidAsync(
        int id,
        VoidReplacementRecordDto dto,
        int userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return Result<bool>.Failure("A non-empty reason is required to void a service replacement record.");
        }

        var record = await _db.ServiceReplacementRecords
            .AsNoTracking()
            .Include(r => r.Generator)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (record is null)
        {
            return Result<bool>.NotFound($"Service replacement record with ID {id} not found.");
        }

        if (!await HasAccessToSubStationAsync(record.Generator.SubStationId, ct))
            return Result<bool>.Forbidden("You do not have access to this generator.");

        if (record.VoidedAtUtc.HasValue)
        {
            return Result<bool>.Failure("This service replacement record has already been voided.", 400);
        }

        var voidResult = await DbTransactionRunner.RunAsync(_db, async trCt =>
        {
            var trackedRecord = await _db.ServiceReplacementRecords
                .FirstOrDefaultAsync(r => r.Id == id, trCt);
            if (trackedRecord is null)
                return new VoidCommitResult(false, "Service replacement record was not found.", 404, 0);
            if (trackedRecord.VoidedAtUtc.HasValue)
                return new VoidCommitResult(false, "This service replacement record has already been voided.", 400, trackedRecord.GeneratorId);

            var now = DateTime.UtcNow;
            trackedRecord.VoidedAtUtc = now;
            trackedRecord.VoidReason = dto.Reason.Trim();
            trackedRecord.UpdatedAt = now;
            trackedRecord.UpdatedBy = userId;

            if (trackedRecord.OilTransactionId.HasValue)
            {
                var original = await _db.OilTransactions
                    .FirstOrDefaultAsync(x => x.Id == trackedRecord.OilTransactionId.Value, trCt);

                if (original is not null)
                {
                    // Audit trail: DO NOT delete or edit original OilTransaction
                    var reversingTx = new OilTransaction
                    {
                        SubStationId = original.SubStationId,
                        OilLookupId = original.OilLookupId,
                        TransactionTypeLookupId = original.TransactionTypeLookupId,
                        TransactionDate = now,
                        QuantityLiters = -original.QuantityLiters,
                        Reference = $"VOID-SRV-{trackedRecord.Id}",
                        Notes = $"Reversal of voided replacement #{trackedRecord.Id} on generator {trackedRecord.GeneratorId}: {trackedRecord.VoidReason}",
                        IsApproved = true,
                        ApprovedById = userId,
                        ApprovedAt = now,
                        CorrectedFromId = null,
                        CreatedAt = now,
                        CreatedBy = userId,
                        UpdatedAt = now,
                        UpdatedBy = userId
                    };
                    _db.OilTransactions.Add(reversingTx);
                }
            }

            await _db.SaveChangesAsync(trCt);
            return new VoidCommitResult(true, null, 200, trackedRecord.GeneratorId);
        }, ct);

        if (!voidResult.Success)
            return Result<bool>.Failure(voidResult.Error!, voidResult.StatusCode);

        try
        {
            await _dueEvaluation.EvaluateGeneratorAsync(voidResult.GeneratorId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate due state after voiding record {RecordId} on generator {GeneratorId}", id, voidResult.GeneratorId);
        }

        return Result<bool>.Success(true);
    }

    public async Task<IReadOnlyList<ServiceReplacementRecordDto>> GetHistoryAsync(
        int generatorId,
        int? serviceItemId = null,
        CancellationToken ct = default)
    {
        var generator = await GetScopedGeneratorAsync(generatorId, ct);
        if (!generator.IsSuccess) return [];

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
            r.CreatedAt,
            r.OilTransactionId)).ToList();
    }

    private async Task<Result<Generator>> GetScopedGeneratorAsync(int generatorId, CancellationToken ct)
    {
        var generator = await _db.Generators.FirstOrDefaultAsync(g => g.Id == generatorId, ct);
        if (generator is null)
            return Result<Generator>.NotFound($"Generator with ID {generatorId} not found.");
        if (!await HasAccessToSubStationAsync(generator.SubStationId, ct))
            return Result<Generator>.Forbidden("You do not have access to this generator.");
        return Result<Generator>.Success(generator);
    }

    private async Task<bool> HasAccessToSubStationAsync(int subStationId, CancellationToken ct)
    {
        if (_currentUser.HasAnyAdminRole) return true;
        var userId = _currentUser.UserId;
        return userId.HasValue && await _scopeFilter.HasAccessToSubStationAsync(userId.Value, subStationId);
    }

    private sealed record VoidCommitResult(bool Success, string? Error, int StatusCode, int GeneratorId);

    private async Task<Result<IdempotencyStartResult>> StartIdempotentCommandAsync<T>(Guid requestId, string operation, T request, int userId, CancellationToken ct)
    {
        if (requestId == Guid.Empty)
            return Result<IdempotencyStartResult>.Failure("ClientRequestId is required for service history commands.");
        var payload = JsonSerializer.Serialize(request);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return await _idempotency.StartAsync(requestId, operation, hash, userId, ct);
    }

    private static Result<T> Replay<T>(IdempotencyStartResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.ResponsePayload))
        {
            var value = JsonSerializer.Deserialize<T>(result.ResponsePayload);
            if (value is not null) return Result<T>.Success(value, result.ResponseStatusCode ?? 200);
        }
        if (result.ResponseStatusCode.HasValue && result.ResponseStatusCode.Value >= 200 && result.ResponseStatusCode.Value < 300)
        {
            return Result<T>.Success(default!, result.ResponseStatusCode.Value);
        }
        return Result<T>.Failure("The original request is still being processed.", 409);
    }
}
