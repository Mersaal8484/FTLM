using System.Security.Claims;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.SubStations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class SubStationService : ISubStationService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SubStationService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, IHttpContextAccessor httpContextAccessor)
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

    public async Task<Result<SubStationDetailDto>> CreateAsync(CreateSubStationDto dto, int userId)
    {
        var stationExists = await _db.Stations.AnyAsync(s => s.Id == dto.StationId);
        if (!stationExists)
            return Result<SubStationDetailDto>.NotFound($"Station {dto.StationId} not found.");

        var duplicate = await _db.SubStations.AnyAsync(ss => ss.StationId == dto.StationId && ss.Code == dto.Code);
        if (duplicate)
            return Result<SubStationDetailDto>.Failure("SubStation with this code already exists in this station.");

        var subStation = new SubStation
        {
            StationId = dto.StationId,
            Name = dto.Name,
            Code = dto.Code,
            Location = dto.Location,
            CapacityKW = dto.CapacityKW,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
        };

        _db.SubStations.Add(subStation);
        await _db.SaveChangesAsync();

        var detail = (await MapToDetailAsync(subStation.Id))!;
        return Result<SubStationDetailDto>.Success(detail);
    }

    public async Task<Result<SubStationDetailDto>> UpdateAsync(int id, UpdateSubStationDto dto, int userId)
    {
        var subStation = await _db.SubStations.FindAsync(id);
        if (subStation is null)
            return Result<SubStationDetailDto>.NotFound($"SubStation {id} not found.");

        if (dto.StationId.HasValue) subStation.StationId = dto.StationId.Value;
        if (dto.Name is not null) subStation.Name = dto.Name;
        if (dto.Code is not null) subStation.Code = dto.Code;
        if (dto.Location is not null) subStation.Location = dto.Location;
        if (dto.CapacityKW.HasValue) subStation.CapacityKW = dto.CapacityKW;
        if (dto.IsActive.HasValue) subStation.IsActive = dto.IsActive.Value;

        subStation.UpdatedAt = DateTime.UtcNow;
        subStation.UpdatedBy = userId;

        await _db.SaveChangesAsync();
        var detail = (await MapToDetailAsync(subStation.Id))!;
        return Result<SubStationDetailDto>.Success(detail);
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var subStation = await _db.SubStations.FindAsync(id);
        if (subStation is null)
            return Result.NotFound($"SubStation {id} not found.");

        subStation.IsActive = false;
        subStation.DeletedAt = DateTime.UtcNow;
        subStation.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<SubStationDetailDto?> GetByIdAsync(int id)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0 || !accessibleIds.Contains(id))
                    return null;
            }
        }

        return await MapToDetailAsync(id);
    }

    public async Task<List<SubStationListDto>> GetAllAsync(bool includeInactive = false)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<SubStationListDto>();
                return await _db.SubStations
                    .AsNoTracking()
                    .Include(ss => ss.Station)
                    .Where(ss => accessibleIds.Contains(ss.Id) && (includeInactive || ss.IsActive))
                    .OrderBy(ss => ss.Station.Name)
                    .ThenBy(ss => ss.Name)
                    .Select(ss => MapToList(ss))
                    .ToListAsync();
            }
        }

        return await _db.SubStations
            .AsNoTracking()
            .Include(ss => ss.Station)
            .Where(ss => includeInactive || ss.IsActive)
            .OrderBy(ss => ss.Station.Name)
            .ThenBy(ss => ss.Name)
            .Select(ss => MapToList(ss))
            .ToListAsync();
    }

    public async Task<List<SubStationListDto>> GetByStationAsync(int stationId, bool includeInactive = false)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<SubStationListDto>();
                return await _db.SubStations
                    .AsNoTracking()
                    .Include(ss => ss.Station)
                    .Where(ss => ss.StationId == stationId && accessibleIds.Contains(ss.Id) && (includeInactive || ss.IsActive))
                    .OrderBy(ss => ss.Name)
                    .Select(ss => MapToList(ss))
                    .ToListAsync();
            }
        }

        return await _db.SubStations
            .AsNoTracking()
            .Include(ss => ss.Station)
            .Where(ss => ss.StationId == stationId && (includeInactive || ss.IsActive))
            .OrderBy(ss => ss.Name)
            .Select(ss => MapToList(ss))
            .ToListAsync();
    }

    private async Task<SubStationDetailDto?> MapToDetailAsync(int id)
    {
        return await _db.SubStations
            .AsNoTracking()
            .Include(ss => ss.Station)
            .Where(ss => ss.Id == id)
            .Select(ss => new SubStationDetailDto(
                ss.Id, ss.StationId, ss.Station.Name,
                ss.Name, ss.Code, ss.Location, ss.CapacityKW,
                ss.IsActive,
                ss.CreatedAt, ss.UpdatedAt))
            .FirstOrDefaultAsync();
    }

    private static SubStationListDto MapToList(SubStation ss)
    {
        return new SubStationListDto(
            ss.Id, ss.StationId, ss.Station.Name,
            ss.Name, ss.Code, ss.Location, ss.CapacityKW,
            ss.IsActive);
    }
}
