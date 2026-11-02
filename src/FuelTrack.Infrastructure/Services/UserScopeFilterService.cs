using FuelTrack.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class UserScopeFilterService : IUserScopeFilterService
{
    private readonly FuelTrackDbContext _db;

    public UserScopeFilterService(FuelTrackDbContext db)
    {
        _db = db;
    }

    public async Task<List<int>> GetAccessibleSubStationIdsAsync(int userId)
    {
        var scopes = await _db.UserScopes
            .Where(s => s.UserId == userId && s.DeletedAt == null)
            .ToListAsync();

        if (scopes.Count == 0)
            return new List<int>();

        if (scopes.Any(s => s.BranchId == null && s.StationId == null && s.SubStationId == null))
        {
            return await _db.SubStations
                .Where(ss => ss.DeletedAt == null && ss.IsActive)
                .Select(ss => ss.Id)
                .ToListAsync();
        }

        var directSubStationIds = scopes
            .Where(s => s.SubStationId.HasValue)
            .Select(s => s.SubStationId!.Value)
            .ToList();

        var stationIds = scopes
            .Where(s => s.StationId.HasValue && !s.SubStationId.HasValue)
            .Select(s => s.StationId!.Value)
            .ToList();

        var branchIds = scopes
            .Where(s => s.BranchId.HasValue && !s.StationId.HasValue && !s.SubStationId.HasValue)
            .Select(s => s.BranchId!.Value)
            .ToList();

        var query = _db.SubStations
            .Where(ss => ss.DeletedAt == null && ss.IsActive);

        if (directSubStationIds.Count > 0)
            query = query.Where(ss => directSubStationIds.Contains(ss.Id));

        if (stationIds.Count > 0)
            query = query.Where(ss => stationIds.Contains(ss.StationId));

        if (branchIds.Count > 0)
            query = query.Where(ss => branchIds.Contains(ss.Station.BranchId));

        return await query
            .Select(ss => ss.Id)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<int>> GetAccessibleStationIdsAsync(int userId)
    {
        var scopes = await _db.UserScopes
            .Where(s => s.UserId == userId && s.DeletedAt == null)
            .ToListAsync();

        if (scopes.Count == 0)
            return new List<int>();

        if (scopes.Any(s => s.BranchId == null && s.StationId == null && s.SubStationId == null))
        {
            return await _db.Stations
                .Where(s => s.DeletedAt == null && s.IsActive)
                .Select(s => s.Id)
                .ToListAsync();
        }

        var stationIds = scopes
            .Where(s => s.StationId.HasValue)
            .Select(s => s.StationId!.Value)
            .ToList();

        var branchIds = scopes
            .Where(s => s.BranchId.HasValue && !s.StationId.HasValue)
            .Select(s => s.BranchId!.Value)
            .ToList();

        var query = _db.Stations
            .Where(s => s.DeletedAt == null && s.IsActive);

        if (stationIds.Count > 0)
            query = query.Where(s => stationIds.Contains(s.Id));

        if (branchIds.Count > 0)
            query = query.Where(s => branchIds.Contains(s.BranchId));

        return await query
            .Select(s => s.Id)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<int>> GetAccessibleBranchIdsAsync(int userId)
    {
        var scopes = await _db.UserScopes
            .Where(s => s.UserId == userId && s.DeletedAt == null)
            .ToListAsync();

        if (scopes.Count == 0)
            return new List<int>();

        if (scopes.Any(s => s.BranchId == null && s.StationId == null && s.SubStationId == null))
        {
            return await _db.Branches
                .Where(b => b.DeletedAt == null && b.IsActive)
                .Select(b => b.Id)
                .ToListAsync();
        }

        var branchIds = scopes
            .Where(s => s.BranchId.HasValue)
            .Select(s => s.BranchId!.Value)
            .ToList();

        if (branchIds.Count == 0)
            return new List<int>();

        return await _db.Branches
            .Where(b => b.DeletedAt == null && b.IsActive && branchIds.Contains(b.Id))
            .Select(b => b.Id)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<int>> GetAccessibleMeterIdsAsync(int userId)
    {
        var accessibleSubStationIds = await GetAccessibleSubStationIdsAsync(userId);
        if (accessibleSubStationIds.Count == 0)
            return new List<int>();

        return await _db.Meters
            .Where(m => m.SubStationId.HasValue && accessibleSubStationIds.Contains(m.SubStationId.Value))
            .Select(m => m.Id)
            .ToListAsync();
    }

    public async Task<List<int>> GetUserIdsWithAccessToSubStationAsync(int subStationId)
    {
        var subStation = await _db.SubStations
            .Where(ss => ss.Id == subStationId)
            .Select(ss => new { ss.StationId, ss.Station.BranchId })
            .FirstOrDefaultAsync();

        if (subStation == null)
            return new List<int>();

        return await _db.UserScopes
            .Where(s => s.DeletedAt == null && (
                s.BranchId == null && s.StationId == null && s.SubStationId == null ||
                s.SubStationId == subStationId ||
                s.StationId == subStation.StationId ||
                s.BranchId == subStation.BranchId))
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<bool> HasAccessToSubStationAsync(int userId, int subStationId)
    {
        var accessibleIds = await GetAccessibleSubStationIdsAsync(userId);
        return accessibleIds.Contains(subStationId);
    }
}
