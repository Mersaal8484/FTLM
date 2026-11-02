using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class InstalledPartService : IInstalledPartService
{
    private readonly FuelTrackDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserScopeFilterService _scopeFilter;

    public InstalledPartService(FuelTrackDbContext db, ICurrentUserService currentUser, IUserScopeFilterService scopeFilter)
    {
        _db = db;
        _currentUser = currentUser;
        _scopeFilter = scopeFilter;
    }

    public async Task<Result<InstalledPartDto>> InstallAsync(CreateInstalledPartDto dto, int userId, CancellationToken ct = default)
    {
        var generator = await _db.Generators.FirstOrDefaultAsync(g => g.Id == dto.GeneratorId && g.DeletedAt == null, ct);
        if (generator is null)
            return Result<InstalledPartDto>.NotFound("Generator not found.");

        var sparePart = await _db.SpareParts
            .Include(sp => sp.UnitLookup)
            .FirstOrDefaultAsync(sp => sp.Id == dto.SparePartId && sp.DeletedAt == null, ct);
        if (sparePart is null)
            return Result<InstalledPartDto>.NotFound("Spare part not found.");

        if (dto.WorkPermitId.HasValue)
        {
            var permit = await _db.WorkPermits.FirstOrDefaultAsync(wp => wp.Id == dto.WorkPermitId.Value && wp.DeletedAt == null, ct);
            if (permit is null)
                return Result<InstalledPartDto>.NotFound("Work permit not found.");
        }

        if (dto.DispatchRequestItemId.HasValue)
        {
            var dispatchItem = await _db.DispatchRequestItems
                .FirstOrDefaultAsync(dri => dri.Id == dto.DispatchRequestItemId.Value && dri.DeletedAt == null, ct);
            if (dispatchItem is null)
                return Result<InstalledPartDto>.NotFound("Dispatch request item not found.");
        }

        var replacementInterval = dto.ReplacementIntervalHours ?? sparePart.ReplacementIntervalHours;
        double? estimatedReplacementAtHours = null;

        if (replacementInterval.HasValue && replacementInterval.Value > 0)
        {
            estimatedReplacementAtHours = dto.InstallationHours + replacementInterval.Value;
        }

        var installedPart = new InstalledPart
        {
            GeneratorId = dto.GeneratorId,
            SparePartId = dto.SparePartId,
            WorkPermitId = dto.WorkPermitId,
            DispatchRequestItemId = dto.DispatchRequestItemId,
            InstallationDate = dto.InstallationDate,
            InstallationHours = dto.InstallationHours,
            ReplacementIntervalHours = replacementInterval,
            EstimatedReplacementAtHours = estimatedReplacementAtHours,
            IsActive = true,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _db.InstalledParts.Add(installedPart);
        await _db.SaveChangesAsync(ct);

        var detail = await MapToDtoAsync(installedPart.Id, ct);
        return Result<InstalledPartDto>.Success(detail!);
    }

    public async Task<Result<InstalledPartDto>> RemoveAsync(int id, RemoveInstalledPartDto dto, int userId, CancellationToken ct = default)
    {
        var installedPart = await _db.InstalledParts
            .Include(ip => ip.SparePart)
            .Include(ip => ip.Generator)
            .FirstOrDefaultAsync(ip => ip.Id == id && ip.DeletedAt == null, ct);

        if (installedPart is null)
            return Result<InstalledPartDto>.NotFound($"Installed part {id} not found.");

        if (!installedPart.IsActive)
            return Result<InstalledPartDto>.Failure("Part is already removed.");

        if (installedPart.EstimatedReplacementAtHours.HasValue && dto.RemovedAtHours < installedPart.EstimatedReplacementAtHours.Value)
        {
            if (string.IsNullOrWhiteSpace(dto.EarlyReplacementReason))
                return Result<InstalledPartDto>.Failure("Early replacement reason is required when removal occurs before estimated replacement hours.");
        }

        installedPart.IsActive = false;
        installedPart.RemovedAt = DateTime.UtcNow;
        installedPart.RemovedAtHours = dto.RemovedAtHours;
        installedPart.EarlyReplacementReason = dto.EarlyReplacementReason;
        installedPart.Notes = string.IsNullOrEmpty(dto.Notes) ? installedPart.Notes : $"{installedPart.Notes}\n{dto.Notes}".Trim();
        installedPart.UpdatedAt = DateTime.UtcNow;
        installedPart.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);

        var detail = await MapToDtoAsync(installedPart.Id, ct);
        return Result<InstalledPartDto>.Success(detail!);
    }

    public async Task<List<InstalledPartDto>> GetAllAsync(int? generatorId = null, CancellationToken ct = default)
    {
        var accessibleIds = await GetAccessibleSubStationIdsAsync();

        var query = _db.InstalledParts
            .AsNoTracking()
            .Include(ip => ip.Generator)
                .ThenInclude(g => g.Controller)
            .Include(ip => ip.SparePart)
                .ThenInclude(sp => sp.UnitLookup)
            .Include(ip => ip.WorkPermit)
            .Where(ip => ip.DeletedAt == null)
            .AsQueryable();

        if (generatorId.HasValue)
            query = query.Where(ip => ip.GeneratorId == generatorId.Value);

        if (accessibleIds != null && accessibleIds.Count > 0)
            query = query.Where(ip => accessibleIds.Contains(ip.Generator.SubStationId));

        return await query
            .OrderByDescending(ip => ip.InstallationDate)
            .Select(ip => MapToDto(ip))
            .ToListAsync(ct);
    }

    private async Task<List<int>> GetAccessibleSubStationIdsAsync()
    {
        if (_currentUser.HasAnyAdminRole)
            return await _db.SubStations
                .Where(s => s.DeletedAt == null)
                .Select(s => s.Id)
                .ToListAsync();

        var currentUserId = _currentUser.UserId;
        if (!currentUserId.HasValue)
            return new List<int>();

        return await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
    }

    private static InstalledPartDto MapToDto(InstalledPart ip)
    {
        return new InstalledPartDto(
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
            ip.EstimatedReplacementAtHours.HasValue && ip.Generator.Controller != null && ip.Generator.Controller.OperatingHours > ip.EstimatedReplacementAtHours.Value,
            ip.IsActive,
            ip.RemovedAt,
            ip.RemovedAtHours,
            ip.EarlyReplacementReason,
            ip.Notes);
    }

    public async Task<List<InstalledPartDto>> GetByGeneratorAsync(int generatorId, bool activeOnly, CancellationToken ct = default)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<InstalledPartDto>();

                var generator = await _db.Generators
                    .FirstOrDefaultAsync(g => g.Id == generatorId && accessibleIds.Contains(g.SubStationId), ct);
                if (generator == null)
                    return new List<InstalledPartDto>();
            }
        }

        var query = _db.InstalledParts
            .AsNoTracking()
            .Include(ip => ip.Generator)
            .Include(ip => ip.SparePart)
            .Include(ip => ip.WorkPermit)
            .Where(ip => ip.GeneratorId == generatorId && ip.DeletedAt == null)
            .AsQueryable();

        if (activeOnly)
            query = query.Where(ip => ip.IsActive);

        return await query
            .OrderByDescending(ip => ip.InstallationDate)
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
                ip.EstimatedReplacementAtHours.HasValue && ip.Generator.Controller != null && ip.Generator.Controller.OperatingHours > ip.EstimatedReplacementAtHours.Value,
                ip.IsActive,
                ip.RemovedAt,
                ip.RemovedAtHours,
                ip.EarlyReplacementReason,
                ip.Notes))
            .ToListAsync(ct);
    }

    public async Task<List<InstalledPartDto>> GetOverdueAsync(int? subStationId, CancellationToken ct = default)
    {
        var query = _db.InstalledParts
            .AsNoTracking()
            .Include(ip => ip.Generator)
            .Include(ip => ip.SparePart)
            .Include(ip => ip.WorkPermit)
            .Where(ip => ip.IsActive && ip.DeletedAt == null && ip.EstimatedReplacementAtHours.HasValue)
            .AsQueryable();

        if (subStationId.HasValue)
        {
            query = query.Where(ip => ip.Generator.SubStationId == subStationId.Value);
            if (!_currentUser.HasAnyAdminRole)
            {
                var currentUserId = _currentUser.UserId;
                if (currentUserId.HasValue)
                {
                    var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                    if (!accessibleIds.Contains(subStationId.Value))
                        return new List<InstalledPartDto>();
                }
            }
        }

        return await query
            .Where(ip => ip.Generator.Controller != null && ip.Generator.Controller.OperatingHours > ip.EstimatedReplacementAtHours)
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
    }

    public async Task<List<InstalledPartDto>> GetUpcomingReplacementsAsync(int? subStationId, double hoursThreshold, CancellationToken ct = default)
    {
        var query = _db.InstalledParts
            .AsNoTracking()
            .Include(ip => ip.Generator)
            .Include(ip => ip.SparePart)
            .Include(ip => ip.WorkPermit)
            .Where(ip => ip.IsActive && ip.DeletedAt == null && ip.EstimatedReplacementAtHours.HasValue)
            .AsQueryable();

        if (subStationId.HasValue)
        {
            query = query.Where(ip => ip.Generator.SubStationId == subStationId.Value);
            if (!_currentUser.HasAnyAdminRole)
            {
                var currentUserId = _currentUser.UserId;
                if (currentUserId.HasValue)
                {
                    var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                    if (!accessibleIds.Contains(subStationId.Value))
                        return new List<InstalledPartDto>();
                }
            }
        }

        return await query
            .Where(ip => ip.Generator.Controller != null &&
                         ip.EstimatedReplacementAtHours.HasValue &&
                         ip.Generator.Controller.OperatingHours >= ip.EstimatedReplacementAtHours.Value - hoursThreshold &&
                         ip.Generator.Controller.OperatingHours < ip.EstimatedReplacementAtHours.Value)
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
                false,
                ip.IsActive,
                ip.RemovedAt,
                ip.RemovedAtHours,
                ip.EarlyReplacementReason,
                ip.Notes))
            .ToListAsync(ct);
    }

    public async Task<bool> CheckEarlyReplacementAsync(int sparePartId, int generatorId, CancellationToken ct = default)
    {
        var installedPart = await _db.InstalledParts
            .Include(ip => ip.Generator)
                .ThenInclude(g => g.Controller)
            .FirstOrDefaultAsync(ip => ip.SparePartId == sparePartId && ip.GeneratorId == generatorId && ip.IsActive && ip.DeletedAt == null, ct);

        if (installedPart == null || !installedPart.EstimatedReplacementAtHours.HasValue)
            return false;

        if (installedPart.Generator.Controller == null)
            return false;

        return installedPart.Generator.Controller.OperatingHours < installedPart.EstimatedReplacementAtHours.Value;
    }

    private async Task<InstalledPartDto?> MapToDtoAsync(int id, CancellationToken ct)
    {
        return await _db.InstalledParts
            .AsNoTracking()
            .Include(ip => ip.Generator)
                .ThenInclude(g => g.Controller)
            .Include(ip => ip.SparePart)
            .Include(ip => ip.WorkPermit)
            .Where(ip => ip.Id == id && ip.DeletedAt == null)
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
                ip.EstimatedReplacementAtHours.HasValue && ip.Generator.Controller != null && ip.Generator.Controller.OperatingHours > ip.EstimatedReplacementAtHours.Value,
                ip.IsActive,
                ip.RemovedAt,
                ip.RemovedAtHours,
                ip.EarlyReplacementReason,
                ip.Notes))
            .FirstOrDefaultAsync(ct);
    }
}
