using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class MaintenanceDashboardService : IMaintenanceDashboardService
{
    private readonly FuelTrackDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserScopeFilterService _scopeFilter;

    public MaintenanceDashboardService(FuelTrackDbContext db, ICurrentUserService currentUser, IUserScopeFilterService scopeFilter)
    {
        _db = db;
        _currentUser = currentUser;
        _scopeFilter = scopeFilter;
    }

    public async Task<MaintenanceDashboardDto> GetDashboardAsync(int? subStationId, int? stationId, CancellationToken ct = default)
    {
        var workPermitQuery = _db.WorkPermits
            .Include(wp => wp.SubStation)
            .Include(wp => wp.Generator)
            .Include(wp => wp.PriorityLookup)
            .Include(wp => wp.StatusLookup)
            .Include(wp => wp.RequestedBy)
            .Include(wp => wp.AssignedTo)
            .Include(wp => wp.Tasks)
            .Where(wp => wp.DeletedAt == null)
            .AsQueryable();

        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleSubStationIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleSubStationIds.Count == 0)
                {
                    return new MaintenanceDashboardDto(0, 0, 0, 0, 0, new List<WorkPermitListDto>(), new List<InstalledPartDto>());
                }
                workPermitQuery = workPermitQuery.Where(wp => accessibleSubStationIds.Contains(wp.SubStationId));
            }
        }

        if (subStationId.HasValue)
            workPermitQuery = workPermitQuery.Where(wp => wp.SubStationId == subStationId.Value);

        if (stationId.HasValue)
        {
            workPermitQuery = workPermitQuery.Where(wp => wp.SubStation.StationId == stationId.Value);
        }

        var activeStatus = await _db.Lookups
            .FirstOrDefaultAsync(l => l.Category.Name == "MaintenanceStatus" && l.Code == "Active", ct);
        var openStatuses = new[] { "Draft", "Submitted", "Active" };

        var openPermitsQuery = workPermitQuery.Where(wp => openStatuses.Contains(wp.StatusLookup.Code));
        var totalOpenPermits = await openPermitsQuery.CountAsync(ct);

        var criticalPermits = await workPermitQuery
            .Where(wp => wp.PriorityLookup.Code == "Critical" && openStatuses.Contains(wp.StatusLookup.Code))
            .CountAsync(ct);

        var activeWorkPermits = await workPermitQuery
            .Where(wp => wp.StatusLookup.Code == "Active")
            .CountAsync(ct);

        var recentPermits = await workPermitQuery
            .OrderByDescending(wp => wp.PermitDate)
            .Take(10)
            .Select(wp => new WorkPermitListDto(
                wp.Id,
                wp.PermitNumber,
                wp.SubStation.Name,
                wp.Generator != null ? wp.Generator.Name : null,
                wp.MaintenanceTypeLookup.Name,
                wp.DepartmentLookup.Name,
                wp.PriorityLookup.Name,
                wp.StatusLookup.Name,
                wp.PermitDate,
                wp.RequestedBy.FullName,
                wp.AssignedTo != null ? wp.AssignedTo.FullName : null,
                wp.PlannedStartDate,
                wp.ActualStartDate,
                wp.Tasks.Count,
                wp.Tasks.Count(t => t.IsCompleted)))
            .ToListAsync(ct);

        var installedPartsQuery = _db.InstalledParts
            .Include(ip => ip.Generator)
            .Include(ip => ip.SparePart)
            .Include(ip => ip.WorkPermit)
            .Where(ip => ip.IsActive && ip.DeletedAt == null)
            .AsQueryable();

        if (subStationId.HasValue)
        {
            installedPartsQuery = installedPartsQuery.Where(ip => ip.Generator.SubStationId == subStationId.Value);
        }

        if (stationId.HasValue)
        {
            installedPartsQuery = installedPartsQuery.Where(ip => ip.Generator.SubStation.StationId == stationId.Value);
        }

        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleSubStationIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleSubStationIds.Count == 0)
                {
                    return new MaintenanceDashboardDto(totalOpenPermits, criticalPermits, 0, 0, activeWorkPermits, recentPermits, new List<InstalledPartDto>());
                }
                installedPartsQuery = installedPartsQuery.Where(ip => accessibleSubStationIds.Contains(ip.Generator.SubStationId));
            }
        }

        var overdueInstalledParts = await installedPartsQuery
            .Where(ip => ip.EstimatedReplacementAtHours.HasValue &&
                         ip.Generator.Controller != null &&
                         ip.Generator.Controller.OperatingHours > ip.EstimatedReplacementAtHours.Value)
            .Select(ip => new InstalledPartDto(
                ip.Id,
                ip.GeneratorId,
                ip.Generator.Name,
                ip.SparePartId,
                ip.SparePart.Name,
                ip.SparePart.PartNumber,
                ip.WorkPermit != null ? ip.WorkPermit.PermitNumber : null,
                ip.InstallationDate,
                ip.InstallationHours,
                ip.ReplacementIntervalHours,
                ip.EstimatedReplacementAtHours,
                ip.Generator.Controller != null ? ip.Generator.Controller.OperatingHours : null,
                ip.EstimatedReplacementAtHours.HasValue ? (double?)(ip.EstimatedReplacementAtHours.Value - (ip.Generator.Controller != null ? ip.Generator.Controller.OperatingHours : 0)) : null,
                true,
                ip.IsActive,
                ip.RemovedAt,
                ip.RemovedAtHours,
                ip.EarlyReplacementReason,
                ip.Notes))
            .ToListAsync(ct);

        var lowStockPartsCount = await _db.WarehouseStocks
            .Where(ws => ws.DeletedAt == null && ws.CurrentQuantity < ws.SparePart.MinStockQuantity)
            .CountAsync(ct);

        return new MaintenanceDashboardDto(
            totalOpenPermits,
            criticalPermits,
            overdueInstalledParts.Count,
            lowStockPartsCount,
            activeWorkPermits,
            recentPermits,
            overdueInstalledParts);
    }
}
