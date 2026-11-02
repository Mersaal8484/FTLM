using FuelTrack.Application.DTOs.Meters;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class MeterService : IMeterService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;

    public MeterService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, ICurrentUserService currentUser)
    {
        _db = db;
        _scopeFilter = scopeFilter;
        _currentUser = currentUser;
    }

    public async Task<MeterDetailDto> CreateAsync(CreateMeterDto dto)
    {
        var meter = new Meter
        {
            Name = dto.MeterName ?? string.Empty,
            SubStationId = dto.SubStationId,
            MeterTypeLookupId = dto.MeterTypeLookupId,
            SerialNumber = dto.SerialNumber,
            GeneratorId = dto.GeneratorId,
            HoursReading = dto.HoursReading,
            EnergyReading = dto.EnergyReading,
            IsActive = dto.IsActive ?? true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Meters.Add(meter);
        await _db.SaveChangesAsync();

        return (await MapToDetailAsync(meter.Id))!;
    }

    public async Task<MeterDetailDto> UpdateAsync(int id, CreateMeterDto dto)
    {
        var meter = await _db.Meters.FindAsync(id)
            ?? throw new KeyNotFoundException($"Meter {id} not found.");

        meter.Name = dto.MeterName ?? meter.Name;
        meter.SubStationId = dto.SubStationId;
        meter.MeterTypeLookupId = dto.MeterTypeLookupId;
        meter.SerialNumber = dto.SerialNumber;
        meter.GeneratorId = dto.GeneratorId;
        meter.HoursReading = dto.HoursReading;
        meter.EnergyReading = dto.EnergyReading;
        if (dto.IsActive.HasValue) meter.IsActive = dto.IsActive.Value;
        meter.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (await MapToDetailAsync(meter.Id))!;
    }

    public async Task<MeterDetailDto?> GetByIdAsync(int id)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return null;
                return await _db.Meters
                    .AsNoTracking()
                    .Include(m => m.SubStation).ThenInclude(ss => ss.Station).ThenInclude(s => s.Branch)
                    .Include(m => m.Generator)
                    .Include(m => m.MeterTypeLookup)
                    .Where(m => m.Id == id && accessibleIds.Contains(m.SubStationId ?? 0))
                    .Select(m => new MeterDetailDto(
                        m.Id,
                        m.Name,
                        m.SubStationId,
                        m.SubStation == null ? null : m.SubStation.Name,
                        m.SubStation != null ? m.SubStation.StationId : null,
                        m.SubStation != null ? m.SubStation.Station.Name : null,
                        m.SubStation != null && m.SubStation.Station != null ? m.SubStation.Station.BranchId : null,
                        m.SubStation != null && m.SubStation.Station != null && m.SubStation.Station.Branch != null ? m.SubStation.Station.Branch.Name : null,
                        m.MeterTypeLookup != null ? m.MeterTypeLookup.Name : null,
                        m.SerialNumber,
                        m.GeneratorId,
                        m.Generator == null ? null : m.Generator.Name,
                        m.HoursReading,
                        m.EnergyReading,
                        m.CreatedAt,
                        m.UpdatedAt,
                        m.IsActive))
                    .FirstOrDefaultAsync();
            }
        }

        return await MapToDetailAsync(id);
    }

    public async Task<List<MeterListDto>> GetAllAsync()
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<MeterListDto>();
                return await _db.Meters
                    .AsNoTracking()
                    .Include(m => m.SubStation).ThenInclude(ss => ss.Station).ThenInclude(s => s.Branch)
                    .Include(m => m.Generator)
                    .Include(m => m.MeterTypeLookup)
                    .Where(m => accessibleIds.Contains(m.SubStationId ?? 0))
                    .OrderBy(m => m.Name)
                    .Select(m => new MeterListDto(
                        m.Id,
                        m.Name,
                        m.SubStationId,
                        m.SubStation == null ? null : m.SubStation.Name,
                        m.SubStation != null ? m.SubStation.StationId : null,
                        m.SubStation != null ? m.SubStation.Station.Name : null,
                        m.SubStation != null && m.SubStation.Station != null ? m.SubStation.Station.BranchId : null,
                        m.SubStation != null && m.SubStation.Station != null && m.SubStation.Station.Branch != null ? m.SubStation.Station.Branch.Name : null,
                        m.MeterTypeLookupId,
                        m.MeterTypeLookup != null ? m.MeterTypeLookup.Name : null,
                        m.SerialNumber,
                        m.GeneratorId,
                        m.Generator == null ? null : m.Generator.Name,
                        m.HoursReading,
                        m.EnergyReading,
                        m.IsActive))
                    .ToListAsync();
            }
        }

        return await _db.Meters
            .AsNoTracking()
            .Include(m => m.SubStation).ThenInclude(ss => ss.Station).ThenInclude(s => s.Branch)
            .Include(m => m.Generator)
            .Include(m => m.MeterTypeLookup)
            .OrderBy(m => m.Name)
            .Select(m => new MeterListDto(
                m.Id,
                m.Name,
                m.SubStationId,
                m.SubStation == null ? null : m.SubStation.Name,
                m.SubStation != null ? m.SubStation.StationId : null,
                m.SubStation != null ? m.SubStation.Station.Name : null,
                m.SubStation != null && m.SubStation.Station != null ? m.SubStation.Station.BranchId : null,
                m.SubStation != null && m.SubStation.Station != null && m.SubStation.Station.Branch != null ? m.SubStation.Station.Branch.Name : null,
                m.MeterTypeLookupId,
                m.MeterTypeLookup != null ? m.MeterTypeLookup.Name : null,
                m.SerialNumber,
                m.GeneratorId,
                m.Generator == null ? null : m.Generator.Name,
                m.HoursReading,
                m.EnergyReading,
                m.IsActive))
            .ToListAsync();
    }

    public async Task<List<MeterListDto>> GetBySubStationAsync(int subStationId)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0 || !accessibleIds.Contains(subStationId))
                    return new List<MeterListDto>();
            }
        }

        return await _db.Meters
            .AsNoTracking()
            .Include(m => m.SubStation).ThenInclude(ss => ss.Station).ThenInclude(s => s.Branch)
            .Include(m => m.Generator)
            .Include(m => m.MeterTypeLookup)
            .Where(m => m.SubStationId == subStationId && m.IsActive == true && m.MeterTypeLookupId == 16)
            .OrderBy(m => m.Name)
            .Select(m => new MeterListDto(
                m.Id,
                m.Name,
                m.SubStationId,
                m.SubStation == null ? null : m.SubStation.Name,
                m.SubStation != null ? m.SubStation.StationId : null,
                m.SubStation != null ? m.SubStation.Station.Name : null,
                m.SubStation != null && m.SubStation.Station != null ? m.SubStation.Station.BranchId : null,
                m.SubStation != null && m.SubStation.Station != null && m.SubStation.Station.Branch != null ? m.SubStation.Station.Branch.Name : null,
                m.MeterTypeLookupId,
                m.MeterTypeLookup != null ? m.MeterTypeLookup.Name : null,
                m.SerialNumber,
                m.GeneratorId,
                m.Generator == null ? null : m.Generator.Name,
                m.HoursReading,
                m.EnergyReading,
                m.IsActive))
            .ToListAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var meter = await _db.Meters.FindAsync(id)
            ?? throw new KeyNotFoundException($"Meter {id} not found.");
        _db.Meters.Remove(meter);
        await _db.SaveChangesAsync();
    }

    private async Task<MeterDetailDto?> MapToDetailAsync(int id)
    {
        return await _db.Meters
            .AsNoTracking()
            .Include(m => m.SubStation).ThenInclude(ss => ss.Station).ThenInclude(s => s.Branch)
            .Include(m => m.Generator)
            .Include(m => m.MeterTypeLookup)
            .Where(m => m.Id == id)
            .Select(m => new MeterDetailDto(
                m.Id,
                m.Name,
                m.SubStationId,
                m.SubStation == null ? null : m.SubStation.Name,
                m.SubStation != null ? m.SubStation.StationId : null,
                m.SubStation != null ? m.SubStation.Station.Name : null,
                m.SubStation != null && m.SubStation.Station != null ? m.SubStation.Station.BranchId : null,
                m.SubStation != null && m.SubStation.Station != null && m.SubStation.Station.Branch != null ? m.SubStation.Station.Branch.Name : null,
                m.MeterTypeLookup != null ? m.MeterTypeLookup.Name : null,
                m.SerialNumber,
                m.GeneratorId,
                m.Generator == null ? null : m.Generator.Name,
                m.HoursReading,
                m.EnergyReading,
                m.CreatedAt,
                m.UpdatedAt,
                m.IsActive))
            .FirstOrDefaultAsync();
    }
}
