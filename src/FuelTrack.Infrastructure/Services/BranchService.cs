using System.Security.Claims;
using FuelTrack.Application.DTOs.Branches;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class BranchService : IBranchService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BranchService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, IHttpContextAccessor httpContextAccessor)
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

    public async Task<BranchDetailDto> CreateAsync(CreateBranchDto dto, int userId)
    {
        var duplicate = await _db.Branches.AnyAsync(b => b.Code == dto.Code);
        if (duplicate)
            throw new InvalidOperationException("A branch with this code already exists.");

        var branch = new Branch
        {
            Name = dto.Name,
            Code = dto.Code,
            IsGeneration = dto.IsGeneration,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
        };

        _db.Branches.Add(branch);
        await _db.SaveChangesAsync();

        return (await MapToDetailAsync(branch.Id))!;
    }

    public async Task<BranchDetailDto> UpdateAsync(int id, UpdateBranchDto dto, int userId)
    {
        var branch = await _db.Branches.FindAsync(id)
            ?? throw new KeyNotFoundException($"Branch {id} not found.");

        if (dto.Name is not null) branch.Name = dto.Name;
        if (dto.Code is not null) branch.Code = dto.Code;
        if (dto.IsGeneration.HasValue) branch.IsGeneration = dto.IsGeneration.Value;
        if (dto.Notes is not null) branch.Notes = dto.Notes;

        branch.UpdatedAt = DateTime.UtcNow;
        branch.UpdatedBy = userId;

        await _db.SaveChangesAsync();
        return (await MapToDetailAsync(branch.Id))!;
    }

    public async Task DeleteAsync(int id)
    {
        var branch = await _db.Branches
            .Include(b => b.Stations)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new KeyNotFoundException($"Branch {id} not found.");

        if (branch.Stations.Any())
            throw new InvalidOperationException("Cannot delete a branch with associated stations.");

        _db.Branches.Remove(branch);
        await _db.SaveChangesAsync();
    }

    public async Task<BranchDetailDto?> GetByIdAsync(int id)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleBranchIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0 || !accessibleIds.Contains(id))
                    return null;
            }
        }

        return await MapToDetailAsync(id);
    }

    public async Task<List<BranchListDto>> GetAllAsync()
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleBranchIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<BranchListDto>();
                return await _db.Branches
                    .AsNoTracking()
                    .Include(b => b.Stations)
                    .Where(b => accessibleIds.Contains(b.Id))
                    .OrderBy(b => b.Name)
                    .Select(b => MapToList(b))
                    .ToListAsync();
            }
        }

        return await _db.Branches
            .AsNoTracking()
            .Include(b => b.Stations)
            .OrderBy(b => b.Name)
            .Select(b => MapToList(b))
            .ToListAsync();
    }

    private async Task<BranchDetailDto?> MapToDetailAsync(int id)
    {
        return await _db.Branches
            .AsNoTracking()
            .Include(b => b.Stations)
            .Where(b => b.Id == id)
            .Select(b => new BranchDetailDto(
                b.Id, b.Name, b.Code,
                b.IsGeneration, b.Notes,
                b.Stations.Count,
                b.CreatedAt, b.UpdatedAt))
            .FirstOrDefaultAsync();
    }

    private static BranchListDto MapToList(Branch b)
    {
        return new BranchListDto(
            b.Id, b.Name, b.Code,
            b.IsGeneration, b.Stations.Count);
    }
}
