using FuelTrack.Application.DTOs.Oils;
using FuelTrack.Application.DTOs.OilTransactions;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class OilService : IOilService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;

    public OilService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, ICurrentUserService currentUser)
    {
        _db = db;
        _scopeFilter = scopeFilter;
        _currentUser = currentUser;
    }

    public async Task<OilDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return null;
                 return await _db.Set<Oil>()
                     .Include(o => o.OilLookup)
                     .Include(o => o.Station)
                     .Include(o => o.SubStation)
                     .Where(o => o.DeletedAt == null && o.Id == id && o.SubStationId.HasValue && accessibleIds.Contains(o.SubStationId.Value))
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
                         o.IsActive,
                         o.CreatedAt,
                         o.UpdatedAt))
                     .FirstOrDefaultAsync(ct);
             }
        }

        return await _db.Set<Oil>()
            .Include(o => o.OilLookup)
            .Include(o => o.Station)
            .Include(o => o.SubStation)
            .Where(o => o.DeletedAt == null && o.Id == id)
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
                o.IsActive,
                o.CreatedAt,
                o.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<OilListDto>> GetAllAsync(CancellationToken ct = default)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<OilListDto>();
                return await _db.Set<Oil>()
                    .Include(o => o.OilLookup)
                    .Include(o => o.Station)
                    .Include(o => o.SubStation)
                    .Where(o => o.DeletedAt == null && o.SubStationId.HasValue && accessibleIds.Contains(o.SubStationId.Value))
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
                        o.SubStation != null ? o.SubStation.Name : null,
                        o.IsActive))
                    .ToListAsync(ct);
            }
        }

        return await _db.Set<Oil>()
            .Include(o => o.OilLookup)
            .Include(o => o.Station)
            .Include(o => o.SubStation)
            .Where(o => o.DeletedAt == null)
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
                o.SubStation != null ? o.SubStation.Name : null,
                o.IsActive))
            .ToListAsync(ct);
    }

    public async Task<List<OilListDto>> GetAllByStationAsync(int stationId, CancellationToken ct = default)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<OilListDto>();
                return await _db.Set<Oil>()
                    .Include(o => o.OilLookup)
                    .Include(o => o.Station)
                    .Include(o => o.SubStation)
                    .Where(o => o.DeletedAt == null && o.StationId == stationId && o.SubStationId.HasValue && accessibleIds.Contains(o.SubStationId.Value))
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
                        o.SubStation != null ? o.SubStation.Name : null,
                        o.IsActive))
                    .ToListAsync(ct);
            }
        }

        return await _db.Set<Oil>()
            .Include(o => o.OilLookup)
            .Include(o => o.Station)
            .Include(o => o.SubStation)
            .Where(o => o.DeletedAt == null && o.StationId == stationId)
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
                o.SubStation != null ? o.SubStation.Name : null,
                o.IsActive))
            .ToListAsync(ct);
    }

    public async Task<List<OilListDto>> GetAllBySubStationAsync(int subStationId, CancellationToken ct = default)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<OilListDto>();
                return await _db.Set<Oil>()
                    .Include(o => o.OilLookup)
                    .Include(o => o.Station)
                    .Include(o => o.SubStation)
                    .Where(o => o.DeletedAt == null && o.SubStationId == subStationId && accessibleIds.Contains(subStationId))
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
                        o.SubStation != null ? o.SubStation.Name : null,
                        o.IsActive))
                    .ToListAsync(ct);
            }
        }

        return await _db.Set<Oil>()
            .Include(o => o.OilLookup)
            .Include(o => o.Station)
            .Include(o => o.SubStation)
            .Where(o => o.DeletedAt == null && o.SubStationId == subStationId)
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
                o.SubStation != null ? o.SubStation.Name : null,
                o.IsActive))
            .ToListAsync(ct);
    }

    public async Task<OilTransactionDetailDto> AddToGeneratorAsync(AddToGeneratorDto dto, int userId, CancellationToken ct = default)
    {
        var generator = await _db.Generators.FindAsync(new object[] { dto.GeneratorId }, ct)
            ?? throw new KeyNotFoundException($"Generator {dto.GeneratorId} not found.");

        var oil = await _db.Oils
            .Include(o => o.OilLookup)
            .FirstOrDefaultAsync(o => o.Id == dto.OilId && o.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Oil {dto.OilId} not found.");

        if (oil.CurrentStock < (decimal)dto.Quantity)
            throw new InvalidOperationException($"Insufficient oil stock. Available: {oil.CurrentStock}, Requested: {dto.Quantity}");

        var oilLookupId = oil.OilLookupId;

        var txTypeCategory = await _db.LookupCategories.FirstOrDefaultAsync(c => c.Name == "OilTransactionType", ct);
        if (txTypeCategory == null)
            throw new InvalidOperationException("OilTransactionType category not found.");

        var inboundTxType = await _db.Lookups.FirstOrDefaultAsync(l => l.CategoryId == txTypeCategory.Id && l.Name == "وارد", ct);
        if (inboundTxType == null)
            throw new InvalidOperationException("وارد transaction type not found.");

        var tx = new OilTransaction
        {
            SubStationId = generator.SubStationId,
            OilLookupId = oilLookupId,
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

        oil.CurrentStock -= (decimal)dto.Quantity;
        oil.UpdatedAt = DateTime.UtcNow;
        oil.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);

        return new OilTransactionDetailDto(
            tx.Id,
            tx.SubStationId, generator.SubStationId.ToString(),
            oilLookupId, oil.OilLookup?.Name ?? "",
            tx.TransactionTypeLookupId, inboundTxType.Name,
            tx.TransactionDate, tx.QuantityLiters,
            tx.Reference,
            tx.SupplierCompany,
            null, null, null, null,
            false,
            null, null,
            null,
            tx.Notes,
            tx.CorrectedFromId,
            tx.CorrectionReason,
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
            IsActive = dto.IsActive,
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
            null,
            oil.IsActive);
    }

    public async Task<OilDetailDto> UpdateAsync(int id, UpdateOilDto dto, int userId, CancellationToken ct = default)
    {
        var oil = await _db.Set<Oil>()
            .Include(o => o.OilLookup)
            .Include(o => o.Station)
            .Include(o => o.SubStation)
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null, ct)
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
        if (dto.IsActive.HasValue) oil.IsActive = dto.IsActive.Value;

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
            oil.IsActive,
            oil.CreatedAt,
            oil.UpdatedAt);
    }

    public async Task DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var oil = await _db.Set<Oil>()
            .FirstOrDefaultAsync(o => o.Id == id && o.IsActive, ct)
            ?? throw new KeyNotFoundException($"Oil with id {id} not found");

        oil.IsActive = false;
        oil.DeletedAt = DateTime.UtcNow;
        oil.UpdatedAt = DateTime.UtcNow;
        oil.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);
    }
}
