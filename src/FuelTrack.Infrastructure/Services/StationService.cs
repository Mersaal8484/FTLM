using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Stations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class StationService : IStationService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;

    public StationService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, ICurrentUserService currentUser)
    {
        _db = db;
        _scopeFilter = scopeFilter;
        _currentUser = currentUser;
    }

    public async Task<Result<StationDetailDto>> CreateAsync(CreateStationDto dto, int userId)
    {
        var branchExists = await _db.Branches.AnyAsync(b => b.Id == dto.BranchId);
        if (!branchExists)
            return Result<StationDetailDto>.NotFound($"Branch {dto.BranchId} not found.");

        var duplicate = await _db.Stations.AnyAsync(s => s.BranchId == dto.BranchId && s.Code == dto.Code);
        if (duplicate)
            return Result<StationDetailDto>.Failure("Station with this code already exists in this branch.");

        var station = new Station
        {
            BranchId = dto.BranchId,
            Name = dto.Name,
            Code = dto.Code,
            Notes = dto.Notes,
            IsActive = dto.IsActive ?? true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
        };

        _db.Stations.Add(station);
        await _db.SaveChangesAsync();

        var detail = (await MapToDetailAsync(station.Id))!;
        return Result<StationDetailDto>.Success(detail);
    }

    public async Task<Result<StationDetailDto>> UpdateAsync(int id, UpdateStationDto dto, int userId)
    {
        var station = await _db.Stations.FindAsync(id);
        if (station is null)
            return Result<StationDetailDto>.NotFound($"Station {id} not found.");

        if (dto.BranchId.HasValue) station.BranchId = dto.BranchId.Value;
        if (dto.Name is not null) station.Name = dto.Name;
        if (dto.Code is not null) station.Code = dto.Code;
        if (dto.Notes is not null) station.Notes = dto.Notes;
        if (dto.IsActive.HasValue) station.IsActive = dto.IsActive.Value;

        station.UpdatedAt = DateTime.UtcNow;
        station.UpdatedBy = userId;

        await _db.SaveChangesAsync();
        var detail = (await MapToDetailAsync(station.Id))!;
        return Result<StationDetailDto>.Success(detail);
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var station = await _db.Stations
            .Include(s => s.SubStations)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (station is null)
            return Result.NotFound($"Station {id} not found.");

        if (station.SubStations.Count != 0)
            return Result.Failure("Cannot delete a station with associated substations.");

        station.IsActive = false;
        station.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<StationDetailDto?> GetByIdAsync(int id)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0 || !accessibleIds.Contains(id))
                    return null;
            }
        }

        return await MapToDetailAsync(id);
    }

    public async Task<List<StationListDto>> GetAllAsync()
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<StationListDto>();
                return await _db.Stations
                    .AsNoTracking()
                    .Include(s => s.Branch)
                    .Where(s => accessibleIds.Contains(s.Id))
                    .OrderBy(s => s.Branch.Name)
                    .ThenBy(s => s.Name)
                    .Select(s => MapToList(s))
                    .ToListAsync();
            }
        }

        return await _db.Stations
            .AsNoTracking()
            .Include(s => s.Branch)
            .OrderBy(s => s.Branch.Name)
            .ThenBy(s => s.Name)
            .Select(s => MapToList(s))
            .ToListAsync();
    }

    public async Task<List<StationListDto>> GetByBranchAsync(int branchId)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<StationListDto>();
                return await _db.Stations
                    .AsNoTracking()
                    .Include(s => s.Branch)
                    .Where(s => s.BranchId == branchId && accessibleIds.Contains(s.Id))
                    .OrderBy(s => s.Name)
                    .Select(s => MapToList(s))
                    .ToListAsync();
            }
        }

        return await _db.Stations
            .AsNoTracking()
            .Include(s => s.Branch)
            .Where(s => s.BranchId == branchId)
            .OrderBy(s => s.Name)
            .Select(s => MapToList(s))
            .ToListAsync();
    }

    private async Task<StationDetailDto?> MapToDetailAsync(int id)
    {
        return await _db.Stations
            .AsNoTracking()
            .Include(s => s.Branch)
            .Where(s => s.Id == id)
            .Select(s => new StationDetailDto(
                s.Id, s.BranchId, s.Branch.Name,
                s.Name, s.Code,
                s.Notes, s.IsActive,
                s.CreatedAt, s.UpdatedAt))
            .FirstOrDefaultAsync();
    }

    private static StationListDto MapToList(Station s)
    {
        return new StationListDto(
            s.Id, s.BranchId, s.Branch.Name,
            s.Name, s.Code, s.IsActive);
    }
}
