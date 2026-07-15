using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Lookups;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class LookupService : ILookupService
{
    private readonly FuelTrackDbContext _db;
    private readonly ICacheService _cache;

    public LookupService(FuelTrackDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<LookupDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var lookup = await _db.Lookups
            .Include(l => l.Category)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        return lookup is null ? null! : MapDetail(lookup);
    }

    public async Task<List<LookupListDto>> GetAllAsync(CancellationToken ct = default)
    {
        var cached = await _cache.GetAsync<List<LookupListDto>>(CacheKeys.LookupsAll, ct);
        if (cached is not null) return cached;

        var result = await _db.Lookups
            .AsNoTracking()
            .Include(l => l.Category)
            .OrderBy(l => l.Category.DisplayName)
            .ThenBy(l => l.Code)
            .Select(l => new LookupListDto(
                l.Id,
                l.Category.DisplayName ?? l.Category.Name,
                l.Name,
                l.Code,
                l.SortOrder,
                l.IsActive))
            .ToListAsync(ct);

        await _cache.SetAsync(CacheKeys.LookupsAll, result, TimeSpan.FromMinutes(10), ct);
        return result;
    }

    public async Task<List<LookupListDto>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.LookupsByCategory(category);
        var cached = await _cache.GetAsync<List<LookupListDto>>(cacheKey, ct);
        if (cached is not null) return cached;

        var categoryEntity = await _db.Set<LookupCategory>()
            .FirstOrDefaultAsync(lc => lc.Name == category || lc.DisplayName == category, ct);

        if (categoryEntity is null)
            return new List<LookupListDto>();

        var result = await _db.Lookups
            .AsNoTracking()
            .Include(l => l.Category)
            .Where(l => l.CategoryId == categoryEntity.Id)
            .OrderBy(l => l.Code)
            .Select(l => new LookupListDto(
                l.Id,
                l.Category.DisplayName ?? l.Category.Name,
                l.Name,
                l.Code,
                l.SortOrder,
                l.IsActive))
            .ToListAsync(ct);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10), ct);
        return result;
    }

    public async Task<LookupListDto> CreateAsync(CreateLookupDto dto, CancellationToken ct = default)
    {
        var categoryEntity = await _db.Set<LookupCategory>()
            .FirstOrDefaultAsync(lc => lc.Name == dto.Category || lc.DisplayName == dto.Category, ct)
            ?? throw new ArgumentException($"Lookup category '{dto.Category}' not found.");

        var duplicate = await _db.Lookups.AnyAsync(l => l.CategoryId == categoryEntity.Id && l.Code == dto.Code, ct);
        if (duplicate)
            throw new InvalidOperationException($"Lookup ({dto.Category}, {dto.Code}) already exists.");

        var lookup = new Lookup
        {
            CategoryId = categoryEntity.Id,
            Name = dto.Name,
            Code = dto.Code,
            SortOrder = 0,
            IsActive = dto.IsActive ?? true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 0,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = 0,
        };

        _db.Lookups.Add(lookup);
        await _db.SaveChangesAsync(ct);

        await InvalidateLookupCacheAsync(ct);

        return new LookupListDto(
            lookup.Id,
            categoryEntity.DisplayName ?? categoryEntity.Name,
            lookup.Name,
            lookup.Code,
            lookup.SortOrder,
            lookup.IsActive);
    }

    public async Task<LookupDetailDto> UpdateAsync(int id, UpdateLookupDto dto, CancellationToken ct = default)
    {
        var lookup = await _db.Lookups
            .Include(l => l.Category)
            .FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new KeyNotFoundException($"Lookup {id} not found.");

        if (dto.Category is not null)
        {
            var categoryEntity = await _db.Set<LookupCategory>()
                .FirstOrDefaultAsync(lc => lc.Name == dto.Category || lc.DisplayName == dto.Category, ct)
                ?? throw new ArgumentException($"Lookup category '{dto.Category}' not found.");
            lookup.CategoryId = categoryEntity.Id;
        }

        if (dto.Name is not null) lookup.Name = dto.Name;
        if (dto.Code is not null) lookup.Code = dto.Code;
        if (dto.IsActive.HasValue) lookup.IsActive = dto.IsActive.Value;

        lookup.UpdatedAt = DateTime.UtcNow;
        lookup.UpdatedBy = 0;

        await _db.SaveChangesAsync(ct);

        await InvalidateLookupCacheAsync(ct);

        return MapDetail(lookup);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var lookup = await _db.Lookups.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException($"Lookup {id} not found.");

        _db.Lookups.Remove(lookup);
        await _db.SaveChangesAsync(ct);

        await InvalidateLookupCacheAsync(ct);
    }

    private static LookupDetailDto MapDetail(Lookup l)
    {
        return new LookupDetailDto(
            l.Id,
            l.Category.DisplayName ?? l.Category.Name,
            l.Name,
            l.Code,
            l.SortOrder,
            l.IsActive,
            l.CreatedAt,
            l.UpdatedAt);
    }

    private async Task InvalidateLookupCacheAsync(CancellationToken ct)
    {
        await _cache.RemoveAsync(CacheKeys.LookupsAll, ct);
        await _cache.RemoveByPrefixAsync("lookups:category:", ct);
    }
}
