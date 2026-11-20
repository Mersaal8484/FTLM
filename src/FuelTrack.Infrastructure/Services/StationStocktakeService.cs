using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Stocktakes;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

/// <summary>Commercial-mode physical stocktake workflow. Approved rows are immutable anchors, never stock mutations.</summary>
public sealed class StationStocktakeService(
    FuelTrackDbContext db,
    ICommercialStationModeService commercialMode,
    IUserScopeFilterService scopeFilter,
    IIdempotencyService idempotency,
    IAlertGeneratorService alerts) : IStationStocktakeService
{
    private const string AttachmentEntityType = "StationStocktake";

    public async Task<Result<StationStocktakeDto>> CreateAsync(CreateStationStocktakeDto dto, int userId, CancellationToken cancellationToken = default)
    {
        var scope = await GetCommercialScopeAsync(cancellationToken);
        if (scope is null) return Result<StationStocktakeDto>.Forbidden("Physical stocktakes are available only in commercial station mode.");
        if (!await CanAccessScopeAsync(userId, scope.SubStationId, cancellationToken)) return Result<StationStocktakeDto>.NotFound("Commercial station was not found.");
        if (dto.FuelLines is null || dto.OilLines is null || dto.AttachmentIds is null) return Result<StationStocktakeDto>.Failure("Stocktake lines and photos are required.");
        if (dto.ClientRequestId == Guid.Empty) return Result<StationStocktakeDto>.Failure("ClientRequestId is required.");
        if ((dto.FuelLines?.Count ?? 0) + (dto.OilLines?.Count ?? 0) == 0) return Result<StationStocktakeDto>.Failure("At least one fuel or oil line is required.");
        if ((dto.AttachmentIds?.Count ?? 0) == 0) return Result<StationStocktakeDto>.Failure("At least one stocktake photo is required.");
        if (dto.FuelLines.GroupBy(x => x.FuelTankId).Any(x => x.Count() > 1) || dto.OilLines.GroupBy(x => x.OilId).Any(x => x.Count() > 1))
            return Result<StationStocktakeDto>.Failure("A tank or oil may appear only once in a stocktake.");
        if (dto.FuelLines.Any(x => x.CountedLiters < 0) || dto.OilLines.Any(x => x.CountedLiters < 0))
            return Result<StationStocktakeDto>.Failure("Counted quantities cannot be negative.");

        var requestHash = Hash(dto);
        var started = await idempotency.StartAsync(dto.ClientRequestId, "station-stocktake", requestHash, userId, cancellationToken);
        if (!started.IsSuccess) return Result<StationStocktakeDto>.Failure(started.Error!, started.StatusCode);
        if (!started.Value!.IsNew)
        {
            if (string.IsNullOrWhiteSpace(started.Value.ResponsePayload)) return Result<StationStocktakeDto>.Conflict("The stocktake request is already being processed.");
            return Result<StationStocktakeDto>.Success(JsonSerializer.Deserialize<StationStocktakeDto>(started.Value.ResponsePayload)!);
        }

        var tankIds = dto.FuelLines.Select(x => x.FuelTankId).ToList();
        var oilIds = dto.OilLines.Select(x => x.OilId).ToList();
        var tanks = await db.FuelTanks.Where(x => tankIds.Contains(x.Id) && x.SubStationId == scope.SubStationId)
            .Select(x => new { x.Id, x.Name, x.ActualStockLiters, x.DeadStockLiters, x.Shape, x.DiameterCm, x.CylinderHeightCm, x.ConeHeightCm, x.MultiplicationFactor }).ToListAsync(cancellationToken);
        var oils = await db.Oils.Where(x => oilIds.Contains(x.Id) && x.SubStationId == scope.SubStationId)
            .Select(x => new { x.Id, x.OilLookupId, x.CurrentStock, OilName = x.OilLookup.Name }).ToListAsync(cancellationToken);
        if (tanks.Count != tankIds.Count || oils.Count != oilIds.Count)
        {
            await idempotency.CompleteAsync(dto.ClientRequestId, 404, null, userId, cancellationToken);
            return Result<StationStocktakeDto>.NotFound("A selected tank or oil was not found in the commercial station.");
        }

        var photoIds = dto.AttachmentIds.Distinct().ToList();
        var validPhotoCount = await db.Attachments.CountAsync(x => photoIds.Contains(x.Id) && x.CreatedBy == userId, cancellationToken);
        if (validPhotoCount != photoIds.Count)
        {
            await idempotency.CompleteAsync(dto.ClientRequestId, 400, null, userId, cancellationToken);
            return Result<StationStocktakeDto>.Failure("Each stocktake photo must be uploaded before submission.");
        }

        var now = DateTime.UtcNow;
        var stocktake = new StationStocktake
        {
            StationId = scope.StationId,
            SubStationId = scope.SubStationId,
            StocktakeDate = dto.StocktakeDate,
            SubmittedByUserId = userId,
            SubmittedAt = now,
            Notes = dto.Notes?.Trim(),
            ClientRequestId = dto.ClientRequestId,
            CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId
        };
        foreach (var line in dto.FuelLines)
        {
            var tank = tanks.Single(x => x.Id == line.FuelTankId);
            var counted = line.DipLevelCm.HasValue && tank.DiameterCm.HasValue && tank.CylinderHeightCm.HasValue && tank.ConeHeightCm.HasValue && tank.MultiplicationFactor.HasValue
                ? Round(TankGeometry.CalculateLitres(tank.Shape ?? TankShape.VerticalCylinderWithConicalBottom, tank.DiameterCm.Value, tank.CylinderHeightCm.Value, tank.ConeHeightCm.Value, tank.MultiplicationFactor.Value, line.DipLevelCm.Value))
                : Round(line.CountedLiters);
            var book = Round((decimal)(tank.ActualStockLiters ?? 0));
            var dead = Round((decimal)(tank.DeadStockLiters ?? 0));
            stocktake.FuelLines.Add(new StationStocktakeFuelLine
            {
                FuelTankId = tank.Id, CountedLiters = counted, BookLiters = book, DeadStockLiters = dead,
                UsableCountedLiters = Math.Max(0, counted - dead), VarianceLiters = counted - book, DipLevelCm = line.DipLevelCm,
                CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId
            });
        }
        foreach (var line in dto.OilLines)
        {
            var oil = oils.Single(x => x.Id == line.OilId);
            var counted = Round(line.CountedLiters);
            var book = Round(oil.CurrentStock);
            stocktake.OilLines.Add(new StationStocktakeOilLine
            {
                OilId = oil.Id, OilLookupId = oil.OilLookupId, CountedLiters = counted, BookLiters = book, VarianceLiters = counted - book,
                CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId
            });
        }

        db.StationStocktakes.Add(stocktake);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var (attachmentId, sortOrder) in photoIds.Select((id, index) => (id, index)))
            db.AttachmentLinks.Add(new AttachmentLink { AttachmentId = attachmentId, EntityType = AttachmentEntityType, EntityId = stocktake.Id, SortOrder = sortOrder, CreatedAt = now, UpdatedAt = now, CreatedBy = userId, UpdatedBy = userId });
        await db.SaveChangesAsync(cancellationToken);

        var result = await MapAsync(stocktake.Id, cancellationToken);
        await idempotency.CompleteAsync(dto.ClientRequestId, 201, JsonSerializer.Serialize(result), userId, cancellationToken);
        return Result<StationStocktakeDto>.Success(result, 201);
    }

    public async Task<Result<StationStocktakeDto>> GetDraftAsync(DateOnly stocktakeDate, int userId, CancellationToken cancellationToken = default)
    {
        var scope = await GetCommercialScopeAsync(cancellationToken);
        if (scope is null) return Result<StationStocktakeDto>.Forbidden("Physical stocktakes are available only in commercial station mode.");
        if (!await CanAccessScopeAsync(userId, scope.SubStationId, cancellationToken)) return Result<StationStocktakeDto>.NotFound("Commercial station was not found.");
        var fuel = await db.FuelTanks.AsNoTracking().Where(x => x.SubStationId == scope.SubStationId)
            .Select(x => new StationStocktakeFuelLineDto(x.Id, x.Name, 0m, (decimal)(x.ActualStockLiters ?? 0), (decimal)(x.DeadStockLiters ?? 0), 0m, -(decimal)(x.ActualStockLiters ?? 0), null))
            .ToListAsync(cancellationToken);
        var oils = await db.Oils.AsNoTracking().Where(x => x.SubStationId == scope.SubStationId)
            .Select(x => new StationStocktakeOilLineDto(x.Id, x.OilLookupId, x.OilLookup.Name, 0m, x.CurrentStock, -x.CurrentStock))
            .ToListAsync(cancellationToken);
        return Result<StationStocktakeDto>.Success(new StationStocktakeDto(0, stocktakeDate, StocktakeStatus.Pending, 0, DateTime.MinValue, null, null, null, null, [], fuel, oils));
    }

    public async Task<Result<StationStocktakeDto>> GetAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        if (!await CanReadAsync(id, userId, cancellationToken)) return Result<StationStocktakeDto>.NotFound("Stocktake was not found.");
        return Result<StationStocktakeDto>.Success(await MapAsync(id, cancellationToken));
    }

    public async Task<Result<StationStocktakeListDto>> ListAsync(StocktakeStatus? status, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize, int userId, CancellationToken cancellationToken = default)
    {
        var scope = await GetCommercialScopeAsync(cancellationToken);
        if (scope is null) return Result<StationStocktakeListDto>.Forbidden("Physical stocktakes are available only in commercial station mode.");
        if (!await CanAccessScopeAsync(userId, scope.SubStationId, cancellationToken)) return Result<StationStocktakeListDto>.NotFound("Commercial station was not found.");
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.StationStocktakes.AsNoTracking().Where(x => x.SubStationId == scope.SubStationId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (fromDate.HasValue) query = query.Where(x => x.StocktakeDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(x => x.StocktakeDate <= toDate.Value);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.StocktakeDate).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new StationStocktakeListItemDto(x.Id, x.StocktakeDate, x.Status, x.SubmittedByUserId, x.SubmittedAt, x.ReviewedAt,
                x.FuelLines.Sum(l => (decimal?)l.VarianceLiters) ?? 0, x.OilLines.Sum(l => (decimal?)l.VarianceLiters) ?? 0)).ToListAsync(cancellationToken);
        return Result<StationStocktakeListDto>.Success(new StationStocktakeListDto(rows, total, page, pageSize));
    }

    public Task<Result<StationStocktakeDto>> ApproveAsync(int id, int userId, CancellationToken cancellationToken = default) => ReviewAsync(id, userId, null, cancellationToken);
    public Task<Result<StationStocktakeDto>> RejectAsync(int id, string reason, int userId, CancellationToken cancellationToken = default) => string.IsNullOrWhiteSpace(reason)
        ? Task.FromResult(Result<StationStocktakeDto>.Failure("A rejection reason is required.")) : ReviewAsync(id, userId, reason.Trim(), cancellationToken);

    public async Task<Result<int>> GetPhotoAsync(int stocktakeId, int attachmentId, int userId, CancellationToken cancellationToken = default)
    {
        if (!await CanReadAsync(stocktakeId, userId, cancellationToken)) return Result<int>.NotFound("Stocktake was not found.");
        var exists = await db.AttachmentLinks.AnyAsync(x => x.AttachmentId == attachmentId && x.EntityType == AttachmentEntityType && x.EntityId == stocktakeId, cancellationToken);
        return exists ? Result<int>.Success(attachmentId) : Result<int>.NotFound("Stocktake photo was not found.");
    }

    private async Task<Result<StationStocktakeDto>> ReviewAsync(int id, int userId, string? rejectionReason, CancellationToken ct)
    {
        var scope = await GetCommercialScopeAsync(ct);
        var stocktake = await db.StationStocktakes.FirstOrDefaultAsync(x => x.Id == id && scope != null && x.SubStationId == scope.SubStationId, ct);
        if (stocktake is null) return Result<StationStocktakeDto>.NotFound("Stocktake was not found.");
        if (!await CanAccessScopeAsync(userId, stocktake.SubStationId, ct)) return Result<StationStocktakeDto>.NotFound("Stocktake was not found.");
        if (stocktake.Status != StocktakeStatus.Pending) return Result<StationStocktakeDto>.Conflict("Stocktake has already been reviewed.");
        if (stocktake.SubmittedByUserId == userId) return Result<StationStocktakeDto>.Forbidden("The submitter cannot review the same stocktake.");
        stocktake.Status = rejectionReason is null ? StocktakeStatus.Approved : StocktakeStatus.Rejected;
        stocktake.RejectionReason = rejectionReason;
        stocktake.ReviewedByUserId = userId; stocktake.ReviewedAt = DateTime.UtcNow; stocktake.UpdatedAt = DateTime.UtcNow; stocktake.UpdatedBy = userId;
        stocktake.ConcurrencyToken = Guid.NewGuid();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<StationStocktakeDto>.Conflict("Stocktake has already been reviewed.");
        }
        if (stocktake.Status == StocktakeStatus.Approved)
            await alerts.CheckStocktakeVariancesAsync(ct);
        return Result<StationStocktakeDto>.Success(await MapAsync(id, ct));
    }

    private async Task<bool> CanReadAsync(int id, int userId, CancellationToken ct)
    {
        var scope = await GetCommercialScopeAsync(ct);
        if (scope is null) return false;
        return await CanAccessScopeAsync(userId, scope.SubStationId, ct)
            && await db.StationStocktakes.AsNoTracking().AnyAsync(x => x.Id == id && x.SubStationId == scope.SubStationId, ct);
    }

    private Task<bool> CanAccessScopeAsync(int userId, int subStationId, CancellationToken ct) =>
        scopeFilter.HasAccessToSubStationAsync(userId, subStationId);

    private async Task<CommercialStationScope?> GetCommercialScopeAsync(CancellationToken ct) =>
        await commercialMode.IsCommercialModeAsync(ct) ? await commercialMode.GetScopeAsync(ct) : null;

    private async Task<StationStocktakeDto> MapAsync(int id, CancellationToken ct)
    {
        var header = await db.StationStocktakes.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.StocktakeDate, x.Status, x.SubmittedByUserId, x.SubmittedAt, x.ReviewedByUserId, x.ReviewedAt, x.RejectionReason, x.Notes }).SingleAsync(ct);
        var photos = await db.AttachmentLinks.AsNoTracking().Where(x => x.EntityType == AttachmentEntityType && x.EntityId == id).OrderBy(x => x.SortOrder).Select(x => x.AttachmentId).ToListAsync(ct);
        var fuel = await db.StationStocktakeFuelLines.AsNoTracking().Where(x => x.StocktakeId == id).Select(x => new StationStocktakeFuelLineDto(x.FuelTankId, x.FuelTank.Name, x.CountedLiters, x.BookLiters, x.DeadStockLiters, x.UsableCountedLiters, x.VarianceLiters, x.DipLevelCm)).ToListAsync(ct);
        var oil = await db.StationStocktakeOilLines.AsNoTracking().Where(x => x.StocktakeId == id).Select(x => new StationStocktakeOilLineDto(x.OilId, x.OilLookupId, x.OilLookup.Name, x.CountedLiters, x.BookLiters, x.VarianceLiters)).ToListAsync(ct);
        return new StationStocktakeDto(header.Id, header.StocktakeDate, header.Status, header.SubmittedByUserId, header.SubmittedAt, header.ReviewedByUserId, header.ReviewedAt, header.RejectionReason, header.Notes, photos, fuel, oil);
    }

    private static decimal Round(decimal value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);
    private static string Hash(CreateStationStocktakeDto dto) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto)))).ToLowerInvariant();
}
