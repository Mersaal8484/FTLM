using System.Security.Claims;
using FuelTrack.Application.DTOs.OilTransactions;
using FuelTrack.Application.DTOs.Oils;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class OilService : IOilService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OilService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _scopeFilter = scopeFilter;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private bool IsAdmin()
    {
        return _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
    }

    public async Task<OilDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return null;
                return await _db.Set<Oil>()
                    .Include(o => o.OilLookup)
                    .Include(o => o.Station)
                    .Include(o => o.SubStation)
                    .Where(o => o.Id == id && o.IsActive && o.SubStationId.HasValue && accessibleIds.Contains(o.SubStationId.Value))
                    .Select(o => new OilDetailDto(
                        o.Id,
                        o.OilLookupId,
                        o.OilLookup.Name,
                        o.CurrentStock,
                        o.MinStock,
                        o.StationId,
                        o.Station != null ? o.Station.Name : null,
                        o.SubStationId,
                        o.SubStation != null ? o.SubStation.Name : null,
                        o.CreatedAt,
                        o.UpdatedAt))
                    .FirstOrDefaultAsync(ct);
            }
        }

        return await _db.Set<Oil>()
            .Include(o => o.OilLookup)
            .Include(o => o.Station)
            .Include(o => o.SubStation)
            .Where(o => o.Id == id && o.IsActive)
            .Select(o => new OilDetailDto(
                o.Id,
                o.OilLookupId,
                o.OilLookup.Name,
                o.CurrentStock,
                o.MinStock,
                o.StationId,
                o.Station != null ? o.Station.Name : null,
                o.SubStationId,
                o.SubStation != null ? o.SubStation.Name : null,
                o.CreatedAt,
                o.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<OilListDto>> GetAllAsync(CancellationToken ct = default)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<OilListDto>();
                return await _db.Set<Oil>()
                    .Include(o => o.OilLookup)
                    .Include(o => o.Station)
                    .Include(o => o.SubStation)
                    .Where(o => o.IsActive && o.SubStationId.HasValue && accessibleIds.Contains(o.SubStationId.Value))
                    .OrderBy(o => o.OilLookup.Name)
                    .Select(o => new OilListDto(
                        o.Id,
                        o.OilLookupId,
                        o.OilLookup.Name,
                        o.CurrentStock,
                        o.MinStock,
                        o.StationId,
                        o.Station != null ? o.Station.Name : null,
                        o.SubStationId,
                        o.SubStation != null ? o.SubStation.Name : null))
                    .ToListAsync(ct);
            }
        }

        return await _db.Set<Oil>()
            .Include(o => o.OilLookup)
            .Include(o => o.Station)
            .Include(o => o.SubStation)
            .Where(o => o.IsActive)
            .OrderBy(o => o.OilLookup.Name)
            .Select(o => new OilListDto(
                o.Id,
                o.OilLookupId,
                o.OilLookup.Name,
                o.CurrentStock,
                o.MinStock,
                o.StationId,
                o.Station != null ? o.Station.Name : null,
                o.SubStationId,
                o.SubStation != null ? o.SubStation.Name : null))
            .ToListAsync(ct);
    }

    public async Task<List<OilListDto>> GetAllByStationAsync(int stationId, CancellationToken ct = default)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<OilListDto>();
                return await _db.Set<Oil>()
                    .Include(o => o.OilLookup)
                    .Include(o => o.Station)
                    .Include(o => o.SubStation)
                    .Where(o => o.IsActive && o.StationId == stationId && o.SubStationId.HasValue && accessibleIds.Contains(o.SubStationId.Value))
                    .OrderBy(o => o.OilLookup.Name)
                    .Select(o => new OilListDto(
                        o.Id,
                        o.OilLookupId,
                        o.OilLookup.Name,
                        o.CurrentStock,
                        o.MinStock,
                        o.StationId,
                        o.Station != null ? o.Station.Name : null,
                        o.SubStationId,
                        o.SubStation != null ? o.SubStation.Name : null))
                    .ToListAsync(ct);
            }
        }

        return await _db.Set<Oil>()
            .Include(o => o.OilLookup)
            .Include(o => o.Station)
            .Include(o => o.SubStation)
            .Where(o => o.IsActive && o.StationId == stationId)
            .OrderBy(o => o.OilLookup.Name)
            .Select(o => new OilListDto(
                o.Id,
                o.OilLookupId,
                o.OilLookup.Name,
                o.CurrentStock,
                o.MinStock,
                o.StationId,
                o.Station != null ? o.Station.Name : null,
                o.SubStationId,
                o.SubStation != null ? o.SubStation.Name : null))
            .ToListAsync(ct);
    }

    public async Task<List<OilListDto>> GetAllBySubStationAsync(int subStationId, CancellationToken ct = default)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<OilListDto>();
                return await _db.Set<Oil>()
                    .Include(o => o.OilLookup)
                    .Include(o => o.Station)
                    .Include(o => o.SubStation)
                    .Where(o => o.IsActive && o.SubStationId == subStationId && accessibleIds.Contains(subStationId))
                    .OrderBy(o => o.OilLookup.Name)
                    .Select(o => new OilListDto(
                        o.Id,
                        o.OilLookupId,
                        o.OilLookup.Name,
                        o.CurrentStock,
                        o.MinStock,
                        o.StationId,
                        o.Station != null ? o.Station.Name : null,
                        o.SubStationId,
                        o.SubStation != null ? o.SubStation.Name : null))
                    .ToListAsync(ct);
            }
        }

        return await _db.Set<Oil>()
            .Include(o => o.OilLookup)
            .Include(o => o.Station)
            .Include(o => o.SubStation)
            .Where(o => o.IsActive && o.SubStationId == subStationId)
            .OrderBy(o => o.OilLookup.Name)
            .Select(o => new OilListDto(
                o.Id,
                o.OilLookupId,
                o.OilLookup.Name,
                o.CurrentStock,
                o.MinStock,
                o.StationId,
                o.Station != null ? o.Station.Name : null,
                o.SubStationId,
                o.SubStation != null ? o.SubStation.Name : null))
            .ToListAsync(ct);
    }

    public async Task<OilTransactionDetailDto> AddToGeneratorAsync(AddToGeneratorDto dto, int userId, CancellationToken ct = default)
    {
        var generator = await _db.Generators.FindAsync(new object[] { dto.GeneratorId }, ct)
            ?? throw new KeyNotFoundException($"Generator {dto.GeneratorId} not found.");

        var oil = await _db.Oils
            .Include(o => o.OilLookup)
            .FirstOrDefaultAsync(o => o.Id == dto.OilId && o.IsActive, ct)
            ?? throw new KeyNotFoundException($"Oil {dto.OilId} not found.");

        if (oil.CurrentStock < (decimal)dto.Quantity)
            throw new InvalidOperationException($"Insufficient oil stock. Available: {oil.CurrentStock}, Requested: {dto.Quantity}");

        // Find oil lookup type for this oil
        var oilLookupId = oil.OilLookupId;

        // Find or create the "وارد" transaction type lookup
        var txTypeCategory = await _db.LookupCategories.FirstOrDefaultAsync(c => c.Name == "OilTransactionType", ct);
        if (txTypeCategory == null)
            throw new InvalidOperationException("OilTransactionType category not found.");

        var inboundTxType = await _db.Lookups.FirstOrDefaultAsync(l => l.CategoryId == txTypeCategory.Id && l.Name == "وارد", ct);
        if (inboundTxType == null)
            throw new InvalidOperationException("وارد transaction type not found.");

        // Create the transaction
        var tx = new OilTransaction
        {
            SubStationId = generator.SubStationId,
            OilLookupId = oilLookupId,
            OilId = dto.OilId,
            TransactionTypeLookupId = inboundTxType.Id,
            TransactionDate = DateTime.UtcNow,
            QuantityLiters = dto.Quantity,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
        };

        _db.OilTransactions.Add(tx);

        // Deduct stock
        oil.CurrentStock -= (decimal)dto.Quantity;
        oil.UpdatedAt = DateTime.UtcNow;
        oil.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);

        // Return the detail
        return new OilTransactionDetailDto(
            tx.Id,
            tx.SubStationId, generator.SubStationId.ToString(),
            oilLookupId, oil.OilLookup?.Name ?? "",
            tx.OilId, oil.OilLookup?.Name,
            inboundTxType.Id, inboundTxType.Name,
            tx.TransactionDate, tx.QuantityLiters,
            null, null, null, null,
            null, null, null,
            null, null, null,
            null, null,
            null, null,
            false,
            null, null,
            null,
            tx.Notes,
            tx.CreatedBy, tx.UpdatedBy,
            tx.CreatedAt, tx.UpdatedAt);
    }

    public async Task<OilListDto> CreateAsync(CreateOilDto dto, int userId, CancellationToken ct = default)
    {
        var lookupExists = await _db.Lookups.AnyAsync(l => l.Id == dto.OilLookupId, ct);
        if (!lookupExists)
            throw new KeyNotFoundException($"Lookup {dto.OilLookupId} not found.");

        var oil = new Oil
        {
            OilLookupId = dto.OilLookupId,
            CurrentStock = dto.CurrentStock,
            MinStock = dto.MinStock,
            StationId = dto.StationId,
            SubStationId = dto.SubStationId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
        };

        _db.Set<Oil>().Add(oil);
        await _db.SaveChangesAsync(ct);

        var lookupName = await _db.Lookups
            .Where(l => l.Id == dto.OilLookupId)
            .Select(l => l.Name)
            .FirstAsync(ct);

        return new OilListDto(
            oil.Id,
            oil.OilLookupId,
            lookupName,
            oil.CurrentStock,
            oil.MinStock,
            oil.StationId,
            null,
            oil.SubStationId,
            null);
    }

    public async Task<OilDetailDto> UpdateAsync(int id, UpdateOilDto dto, int userId, CancellationToken ct = default)
    {
        var oil = await _db.Set<Oil>()
            .Include(o => o.OilLookup)
            .Include(o => o.Station)
            .Include(o => o.SubStation)
            .FirstOrDefaultAsync(o => o.Id == id && o.IsActive, ct)
            ?? throw new KeyNotFoundException($"Oil with id {id} not found");

        if (dto.OilLookupId.HasValue)
        {
            var lookupExists = await _db.Lookups.AnyAsync(l => l.Id == dto.OilLookupId.Value, ct);
            if (!lookupExists)
                throw new KeyNotFoundException($"Lookup {dto.OilLookupId} not found.");
            oil.OilLookupId = dto.OilLookupId.Value;
        }
        if (dto.CurrentStock.HasValue) oil.CurrentStock = dto.CurrentStock.Value;
        if (dto.MinStock.HasValue) oil.MinStock = dto.MinStock.Value;
        if (dto.StationId is not null) oil.StationId = dto.StationId;
        if (dto.SubStationId is not null) oil.SubStationId = dto.SubStationId;

        oil.UpdatedAt = DateTime.UtcNow;
        oil.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);

        return new OilDetailDto(
            oil.Id,
            oil.OilLookupId,
            oil.OilLookup.Name,
            oil.CurrentStock,
            oil.MinStock,
            oil.StationId,
            oil.Station?.Name,
            oil.SubStationId,
            oil.SubStation?.Name,
            oil.CreatedAt,
            oil.UpdatedAt);
    }

    public async Task DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var oil = await _db.Set<Oil>()
            .FirstOrDefaultAsync(o => o.Id == id && o.IsActive, ct)
            ?? throw new KeyNotFoundException($"Oil with id {id} not found");

        oil.IsActive = false;
        oil.UpdatedAt = DateTime.UtcNow;
        oil.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);
    }
}
