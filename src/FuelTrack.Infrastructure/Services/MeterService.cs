using FuelTrack.Application.DTOs.Meters;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class MeterService : IMeterService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;
    private readonly ICommercialStationModeService _commercialMode;

    public MeterService(
        FuelTrackDbContext db,
        IUserScopeFilterService scopeFilter,
        ICurrentUserService currentUser,
        ICommercialStationModeService commercialMode)
    {
        _db = db;
        _scopeFilter = scopeFilter;
        _currentUser = currentUser;
        _commercialMode = commercialMode;
    }

    private async Task<int?> ResolveMeterTypeLookupIdAsync(string meterType)
    {
        var category = await _db.LookupCategories.FirstOrDefaultAsync(c => c.Name == "MeterType");
        if (category == null) return null;

        var lookup = await _db.Lookups.FirstOrDefaultAsync(l =>
            l.CategoryId == category.Id && (l.Code == meterType || l.Name == meterType));

        if (lookup == null && (meterType == "SolarProductionMeter" || meterType == "SolarMeter" || meterType == "عداد إنتاج الطاقة الشمسية"))
        {
            lookup = new Lookup
            {
                CategoryId = category.Id,
                Code = "SolarProductionMeter",
                Name = "عداد إنتاج الطاقة الشمسية",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.Lookups.Add(lookup);
            await _db.SaveChangesAsync();
        }

        return lookup?.Id;
    }

    public async Task<MeterDetailDto> CreateAsync(CreateMeterDto dto)
    {
        var commercialScope = await GetCommercialScopeAsync();
        var meterTypeLookupId = dto.MeterTypeLookupId;
        if (!meterTypeLookupId.HasValue && !string.IsNullOrWhiteSpace(dto.MeterType))
        {
            meterTypeLookupId = await ResolveMeterTypeLookupIdAsync(dto.MeterType!);
        }

        var meter = new Meter
        {
            Name = dto.MeterName ?? string.Empty,
            SubStationId = commercialScope?.SubStationId ?? dto.SubStationId,
            MeterTypeLookupId = meterTypeLookupId,
            SerialNumber = dto.SerialNumber,
            GeneratorId = commercialScope is null ? dto.GeneratorId : null,
            HoursReading = dto.HoursReading,
            EnergyReading = dto.EnergyReading,
            IsActive = dto.IsActive ?? true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Meters.Add(meter);
        await _db.SaveChangesAsync();

        if (commercialScope is not null)
        {
            var isSolarProduction = false;
            if (meter.MeterTypeLookupId.HasValue)
            {
                var lookup = await _db.Lookups.FindAsync(meter.MeterTypeLookupId.Value);
                if (lookup != null && (lookup.Code == "SolarProductionMeter" || lookup.Code == "SolarMeter"))
                {
                    isSolarProduction = true;
                }
            }

            if (!isSolarProduction)
            {
                var subStationId = meter.SubStationId;
                if (subStationId.HasValue)
                {
                    var feeder = new Feeder
                    {
                        SubStationId = subStationId.Value,
                        Code = $"FDR-{meter.Id}",
                        Name = string.IsNullOrWhiteSpace(meter.Name) ? $"Feeder {meter.Id}" : meter.Name,
                        MeterMultiplier = 1m,
                        MeterId = meter.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                    _db.Feeders.Add(feeder);
                    await _db.SaveChangesAsync();
                }
            }
        }

        return (await MapToDetailAsync(meter.Id))!;
    }

    public async Task<MeterDetailDto> UpdateAsync(int id, CreateMeterDto dto)
    {
        var meter = await _db.Meters.FindAsync(id)
            ?? throw new KeyNotFoundException($"Meter {id} not found.");

        var commercialScope = await GetCommercialScopeAsync();
        if (commercialScope is not null && meter.SubStationId != commercialScope.SubStationId)
            throw new KeyNotFoundException($"Meter {id} not found.");

        var meterTypeLookupId = dto.MeterTypeLookupId;
        if (!meterTypeLookupId.HasValue && !string.IsNullOrWhiteSpace(dto.MeterType))
        {
            meterTypeLookupId = await ResolveMeterTypeLookupIdAsync(dto.MeterType!);
        }

        meter.Name = dto.MeterName ?? meter.Name;
        meter.SubStationId = commercialScope?.SubStationId ?? dto.SubStationId;
        if (meterTypeLookupId.HasValue) meter.MeterTypeLookupId = meterTypeLookupId.Value;
        meter.SerialNumber = dto.SerialNumber;
        meter.GeneratorId = commercialScope is null ? dto.GeneratorId : null;
        meter.HoursReading = dto.HoursReading;
        meter.EnergyReading = dto.EnergyReading;
        if (dto.IsActive.HasValue) meter.IsActive = dto.IsActive.Value;
        meter.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        if (commercialScope is not null)
        {
            var feeder = await _db.Feeders.FirstOrDefaultAsync(f => f.MeterId == meter.Id && f.DeletedAt == null);
            if (feeder is not null)
            {
                feeder.Name = meter.Name;
                if (meter.SubStationId.HasValue) feeder.SubStationId = meter.SubStationId.Value;
                feeder.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        return (await MapToDetailAsync(meter.Id))!;
    }

    public async Task<MeterDetailDto?> GetByIdAsync(int id)
    {
        var commercialScope = await GetCommercialScopeAsync();
        if (commercialScope is not null)
            return await MapToDetailAsync(id, commercialScope.SubStationId);

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
        var commercialScope = await GetCommercialScopeAsync();
        if (commercialScope is not null)
        {
            return await _db.Meters
                .AsNoTracking()
                .Include(m => m.SubStation).ThenInclude(ss => ss.Station).ThenInclude(s => s.Branch)
                .Include(m => m.Generator)
                .Include(m => m.MeterTypeLookup)
                .Where(m => m.SubStationId == commercialScope.SubStationId)
                .OrderBy(m => m.Name)
                .Select(m => new MeterListDto(
                    m.Id, m.Name, m.SubStationId,
                    m.SubStation == null ? null : m.SubStation.Name,
                    m.SubStation != null ? m.SubStation.StationId : null,
                    m.SubStation != null ? m.SubStation.Station.Name : null,
                    m.SubStation != null && m.SubStation.Station != null ? m.SubStation.Station.BranchId : null,
                    m.SubStation != null && m.SubStation.Station != null && m.SubStation.Station.Branch != null ? m.SubStation.Station.Branch.Name : null,
                    m.MeterTypeLookupId, m.MeterTypeLookup != null ? m.MeterTypeLookup.Name : null,
                    m.SerialNumber, m.GeneratorId, m.Generator == null ? null : m.Generator.Name,
                    m.HoursReading, m.EnergyReading, m.IsActive))
                .ToListAsync();
        }

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
        var commercialScope = await GetCommercialScopeAsync();
        if (commercialScope is not null)
            subStationId = commercialScope.SubStationId;

        if (commercialScope is null && !_currentUser.HasAnyAdminRole)
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

        var commercialScope = await GetCommercialScopeAsync();
        if (commercialScope is not null && meter.SubStationId != commercialScope.SubStationId)
            throw new KeyNotFoundException($"Meter {id} not found.");

        if (commercialScope is not null)
        {
            var feeder = await _db.Feeders.FirstOrDefaultAsync(f => f.MeterId == id && f.DeletedAt == null);
            if (feeder is not null)
            {
                feeder.DeletedAt = DateTime.UtcNow;
                feeder.UpdatedAt = DateTime.UtcNow;
            }
        }

        _db.Meters.Remove(meter);
        await _db.SaveChangesAsync();
    }

    private async Task<CommercialStationScope?> GetCommercialScopeAsync()
    {
        if (!await _commercialMode.IsCommercialModeAsync())
            return null;

        return await _commercialMode.GetScopeAsync()
            ?? throw new InvalidOperationException("Commercial mode has no active technical station scope.");
    }

    private async Task<MeterDetailDto?> MapToDetailAsync(int id, int? requiredSubStationId = null)
    {
        return await _db.Meters
            .AsNoTracking()
            .Include(m => m.SubStation).ThenInclude(ss => ss.Station).ThenInclude(s => s.Branch)
            .Include(m => m.Generator)
            .Include(m => m.MeterTypeLookup)
            .Where(m => m.Id == id && (!requiredSubStationId.HasValue || m.SubStationId == requiredSubStationId.Value))
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
