using FuelTrack.Application.Interfaces;
using FuelTrack.Infrastructure;
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

        var subStationIds = new HashSet<int>();

        foreach (var scope in scopes)
        {
            if (scope.SubStationId.HasValue)
            {
                subStationIds.Add(scope.SubStationId.Value);
            }
            else if (scope.StationId.HasValue)
            {
                var ids = await _db.SubStations
                    .Where(ss => ss.StationId == scope.StationId.Value && ss.DeletedAt == null && ss.IsActive)
                    .Select(ss => ss.Id)
                    .ToListAsync();
                foreach (var id in ids) subStationIds.Add(id);
            }
            else if (scope.BranchId.HasValue)
            {
                var ids = await _db.SubStations
                    .Where(ss => ss.Station.BranchId == scope.BranchId.Value && ss.DeletedAt == null && ss.IsActive)
                    .Select(ss => ss.Id)
                    .ToListAsync();
                foreach (var id in ids) subStationIds.Add(id);
            }
        }

        return subStationIds.ToList();
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

        var stationIds = new HashSet<int>();

        foreach (var scope in scopes)
        {
            if (scope.StationId.HasValue)
            {
                stationIds.Add(scope.StationId.Value);
            }
            else if (scope.BranchId.HasValue)
            {
                var ids = await _db.Stations
                    .Where(s => s.BranchId == scope.BranchId.Value && s.DeletedAt == null && s.IsActive)
                    .Select(s => s.Id)
                    .ToListAsync();
                foreach (var id in ids) stationIds.Add(id);
            }
        }

        return stationIds.ToList();
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

        var branchIds = new HashSet<int>();

        foreach (var scope in scopes)
        {
            if (scope.BranchId.HasValue)
                branchIds.Add(scope.BranchId.Value);
        }

        return branchIds.ToList();
    }

    public async Task<bool> HasAccessToSubStationAsync(int userId, int subStationId)
    {
        var accessibleIds = await GetAccessibleSubStationIdsAsync(userId);
        return accessibleIds.Contains(subStationId);
    }
}
