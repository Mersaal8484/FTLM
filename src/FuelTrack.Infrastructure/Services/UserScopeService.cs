using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

/// <summary>
/// Centralized service for UserScope operations.
/// Provides methods for checking scope access and getting accessible entities.
/// 
/// Scope Hierarchy: Branch → Station → SubStation
/// Generators, FuelTanks, OilTanks, Meters are accessed automatically through their SubStation.
/// </summary>
public class UserScopeService
{
    private readonly FuelTrackDbContext _db;

    public UserScopeService(FuelTrackDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Checks if a user has access to a specific branch.
    /// </summary>
    public async Task<bool> CanAccessBranchAsync(int userId, int branchId)
    {
        var accessibleBranchIds = await GetAccessibleBranchIdsAsync(userId);
        return accessibleBranchIds.Contains(branchId);
    }

    /// <summary>
    /// Checks if a user has access to a specific station.
    /// </summary>
    public async Task<bool> CanAccessStationAsync(int userId, int stationId)
    {
        var accessibleStationIds = await GetAccessibleStationIdsAsync(userId);
        return accessibleStationIds.Contains(stationId);
    }

    /// <summary>
    /// Checks if a user has access to a specific sub-station.
    /// </summary>
    public async Task<bool> CanAccessSubStationAsync(int userId, int subStationId)
    {
        var accessibleSubStationIds = await GetAccessibleSubStationIdsAsync(userId);
        return accessibleSubStationIds.Contains(subStationId);
    }

    /// <summary>
    /// Checks if a user has access to a generator (accessed through SubStation).
    /// </summary>
    public async Task<bool> CanAccessGeneratorAsync(int userId, int generatorId)
    {
        var generator = await _db.Generators.FindAsync(generatorId);
        if (generator == null) return false;

        return await CanAccessSubStationAsync(userId, generator.SubStationId);
    }

    /// <summary>
    /// Checks if a user has access to a fuel tank (accessed through SubStation).
    /// </summary>
    public async Task<bool> CanAccessFuelTankAsync(int userId, int fuelTankId)
    {
        var fuelTank = await _db.FuelTanks.FindAsync(fuelTankId);
        if (fuelTank == null) return false;

        return await CanAccessSubStationAsync(userId, fuelTank.SubStationId);
    }

    /// <summary>
    /// Checks if a user has access to an oil tank (accessed through SubStation).
    /// </summary>
    public async Task<bool> CanAccessOilTankAsync(int userId, int oilTankId)
    {
        var oil = await _db.Oils.FindAsync(oilTankId);
        if (oil == null) return false;

        return await CanAccessSubStationAsync(userId, oil.SubStationId ?? 0);
    }

    /// <summary>
    /// Checks if a user has access to a meter (accessed through SubStation).
    /// </summary>
    public async Task<bool> CanAccessMeterAsync(int userId, int meterId)
    {
        var meter = await _db.Meters.FindAsync(meterId);
        if (meter == null) return false;

        return await CanAccessSubStationAsync(userId, meter.SubStationId ?? 0);
    }

    /// <summary>
    /// Gets all accessible branch IDs for a user.
    /// </summary>
    public async Task<List<int>> GetAccessibleBranchIdsAsync(int userId)
    {
        var scopes = await _db.UserScopes
            .Where(s => s.UserId == userId && s.DeletedAt == null)
            .ToListAsync();

        if (scopes.Count == 0)
            return new List<int>();

        // If any scope has all nulls, user has access to all branches (Admin)
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

    /// <summary>
    /// Gets all accessible station IDs for a user.
    /// </summary>
    public async Task<List<int>> GetAccessibleStationIdsAsync(int userId)
    {
        var scopes = await _db.UserScopes
            .Where(s => s.UserId == userId && s.DeletedAt == null)
            .ToListAsync();

        if (scopes.Count == 0)
            return new List<int>();

        // If any scope has all nulls, user has access to all stations (Admin)
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

    /// <summary>
    /// Gets all accessible sub-station IDs for a user.
    /// </summary>
    public async Task<List<int>> GetAccessibleSubStationIdsAsync(int userId)
    {
        var scopes = await _db.UserScopes
            .Where(s => s.UserId == userId && s.DeletedAt == null)
            .ToListAsync();

        if (scopes.Count == 0)
            return new List<int>();

        // If any scope has all nulls, user has access to all sub-stations (Admin)
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

    /// <summary>
    /// Gets all accessible generator IDs for a user (through SubStation hierarchy).
    /// </summary>
    public async Task<List<int>> GetAccessibleGeneratorIdsAsync(int userId)
    {
        var subStationIds = await GetAccessibleSubStationIdsAsync(userId);

        if (subStationIds.Count == 0)
            return new List<int>();

        // Check if user has "All" access
        var scopes = await _db.UserScopes
            .Where(s => s.UserId == userId && s.DeletedAt == null)
            .ToListAsync();

        if (scopes.Any(s => s.BranchId == null && s.StationId == null && s.SubStationId == null))
        {
            return await _db.Generators
                .Where(g => g.DeletedAt == null && g.IsActive)
                .Select(g => g.Id)
                .ToListAsync();
        }

        return await _db.Generators
            .Where(g => subStationIds.Contains(g.SubStationId) && g.DeletedAt == null && g.IsActive)
            .Select(g => g.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all accessible fuel tank IDs for a user (through SubStation hierarchy).
    /// </summary>
    public async Task<List<int>> GetAccessibleFuelTankIdsAsync(int userId)
    {
        var subStationIds = await GetAccessibleSubStationIdsAsync(userId);

        if (subStationIds.Count == 0)
            return new List<int>();

        // Check if user has "All" access
        var scopes = await _db.UserScopes
            .Where(s => s.UserId == userId && s.DeletedAt == null)
            .ToListAsync();

        if (scopes.Any(s => s.BranchId == null && s.StationId == null && s.SubStationId == null))
        {
            return await _db.FuelTanks
                .Where(ft => ft.DeletedAt == null && ft.IsActive)
                .Select(ft => ft.Id)
                .ToListAsync();
        }

        return await _db.FuelTanks
            .Where(ft => subStationIds.Contains(ft.SubStationId) && ft.DeletedAt == null && ft.IsActive)
            .Select(ft => ft.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all accessible oil tank IDs for a user (through SubStation hierarchy).
    /// </summary>
    public async Task<List<int>> GetAccessibleOilTankIdsAsync(int userId)
    {
        var subStationIds = await GetAccessibleSubStationIdsAsync(userId);

        if (subStationIds.Count == 0)
            return new List<int>();

        // Check if user has "All" access
        var scopes = await _db.UserScopes
            .Where(s => s.UserId == userId && s.DeletedAt == null)
            .ToListAsync();

        if (scopes.Any(s => s.BranchId == null && s.StationId == null && s.SubStationId == null))
        {
            return await _db.Oils
                .Where(o => o.DeletedAt == null && o.IsActive)
                .Select(o => o.Id)
                .ToListAsync();
        }

        return await _db.Oils
            .Where(o => o.SubStationId.HasValue && subStationIds.Contains(o.SubStationId.Value) && o.DeletedAt == null && o.IsActive)
            .Select(o => o.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all accessible meter IDs for a user (through SubStation hierarchy).
    /// </summary>
    public async Task<List<int>> GetAccessibleMeterIdsAsync(int userId)
    {
        var subStationIds = await GetAccessibleSubStationIdsAsync(userId);

        if (subStationIds.Count == 0)
            return new List<int>();

        // Check if user has "All" access
        var scopes = await _db.UserScopes
            .Where(s => s.UserId == userId && s.DeletedAt == null)
            .ToListAsync();

        if (scopes.Any(s => s.BranchId == null && s.StationId == null && s.SubStationId == null))
        {
            return await _db.Meters
                .Where(m => m.DeletedAt == null && m.IsActive)
                .Select(m => m.Id)
                .ToListAsync();
        }

        return await _db.Meters
            .Where(m => m.SubStationId.HasValue && subStationIds.Contains(m.SubStationId.Value) && m.DeletedAt == null && m.IsActive)
            .Select(m => m.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Checks if a user is an Admin (has access to all branches, stations, sub-stations).
    /// </summary>
    public async Task<bool> IsAdminAsync(int userId)
    {
        return await _db.UserScopes
            .AnyAsync(s => s.UserId == userId 
                && s.BranchId == null 
                && s.StationId == null 
                && s.SubStationId == null 
                && s.DeletedAt == null);
    }

    /// <summary>
    /// Applies scope filtering to a query based on the module's scope type.
    /// </summary>
    public IQueryable<T> ApplyScope<T>(IQueryable<T> query, int userId, ScopeType scopeType, Func<T, int> getId, Func<T, int?>? getSubStationId = null, Func<T, int?>? getStationId = null, Func<T, int?>? getBranchId = null)
    {
        return scopeType switch
        {
            ScopeType.None => query,
            ScopeType.Branch => query.Where(x => getBranchId != null && GetAccessibleBranchIdsAsync(userId).Result.Contains(getBranchId(x) ?? 0)),
            ScopeType.Station => query.Where(x => getStationId != null && GetAccessibleStationIdsAsync(userId).Result.Contains(getStationId(x) ?? 0)),
            ScopeType.SubStation => query.Where(x => getSubStationId != null && GetAccessibleSubStationIdsAsync(userId).Result.Contains(getSubStationId(x) ?? 0)),
            _ => query
        };
    }
}
