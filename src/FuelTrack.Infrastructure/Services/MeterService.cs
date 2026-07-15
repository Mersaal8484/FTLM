using System.Security.Claims;
using FuelTrack.Application.DTOs.Meters;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class MeterService : IMeterService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MeterService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, IHttpContextAccessor httpContextAccessor)
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

    public async Task<MeterDetailDto> CreateAsync(CreateMeterDto dto)
    {
        var meter = new Meter
        {
            Name = dto.MeterName ?? string.Empty,
            Code = dto.Code ?? string.Empty,
            StationId = dto.StationId,
            SubStationId = dto.SubStationId,
            MeterType = dto.MeterType,
            MeterTypeLookupId = dto.MeterTypeLookupId,
            SerialNumber = dto.SerialNumber,
            GeneratorId = dto.GeneratorId,
            FuelTankId = dto.FuelTankId,
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
        meter.Code = dto.Code ?? meter.Code;
        meter.StationId = dto.StationId;
        meter.SubStationId = dto.SubStationId;
        meter.MeterType = dto.MeterType;
        meter.MeterTypeLookupId = dto.MeterTypeLookupId;
        meter.SerialNumber = dto.SerialNumber;
        meter.GeneratorId = dto.GeneratorId;
        meter.FuelTankId = dto.FuelTankId;
        meter.HoursReading = dto.HoursReading;
        meter.EnergyReading = dto.EnergyReading;
        if (dto.IsActive.HasValue) meter.IsActive = dto.IsActive.Value;
        meter.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (await MapToDetailAsync(meter.Id))!;
    }

    public async Task<MeterDetailDto?> GetByIdAsync(int id)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return null;
                return await _db.Meters
                    .AsNoTracking()
                    .Include(m => m.Station)
                    .Include(m => m.SubStation)
                    .Include(m => m.Generator)
                    .Include(m => m.FuelTank)
                    .Include(m => m.MeterTypeLookup)
                    .Where(m => m.Id == id && accessibleIds.Contains(m.SubStationId ?? 0))
                    .Select(m => new MeterDetailDto(
                        m.Id,
                        m.Name,
                        m.Code,
                        m.StationId,
                        m.Station == null ? null : m.Station.Name,
                        m.SubStationId,
                        m.SubStation == null ? null : m.SubStation.Name,
                        m.MeterType,
                        m.MeterTypeLookup != null ? m.MeterTypeLookup.Name : null,
                        m.SerialNumber,
                        m.GeneratorId,
                        m.Generator == null ? null : m.Generator.Name,
                        m.FuelTankId,
                        m.FuelTank == null ? null : m.FuelTank.Name,
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
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<MeterListDto>();
                return await _db.Meters
                    .AsNoTracking()
                    .Include(m => m.Station)
                    .Include(m => m.SubStation)
                    .Include(m => m.Generator)
                    .Include(m => m.FuelTank)
                    .Include(m => m.MeterTypeLookup)
                    .Where(m => accessibleIds.Contains(m.SubStationId ?? 0))
                    .OrderBy(m => m.Name)
                    .Select(m => new MeterListDto(
                        m.Id,
                        m.Name,
                        m.Code,
                        m.StationId,
                        m.Station == null ? null : m.Station.Name,
                        m.SubStationId,
                        m.SubStation == null ? null : m.SubStation.Name,
                        m.MeterType,
                        m.MeterTypeLookup != null ? m.MeterTypeLookup.Name : null,
                        m.SerialNumber,
                        m.GeneratorId,
                        m.Generator == null ? null : m.Generator.Name,
                        m.FuelTankId,
                        m.FuelTank == null ? null : m.FuelTank.Name,
                        m.HoursReading,
                        m.EnergyReading,
                        m.IsActive))
                    .ToListAsync();
            }
        }

        return await _db.Meters
            .AsNoTracking()
            .Include(m => m.Station)
            .Include(m => m.SubStation)
            .Include(m => m.Generator)
            .Include(m => m.FuelTank)
            .Include(m => m.MeterTypeLookup)
            .OrderBy(m => m.Name)
            .Select(m => new MeterListDto(
                m.Id,
                m.Name,
                m.Code,
                m.StationId,
                m.Station == null ? null : m.Station.Name,
                m.SubStationId,
                m.SubStation == null ? null : m.SubStation.Name,
                m.MeterType,
                m.MeterTypeLookup != null ? m.MeterTypeLookup.Name : null,
                m.SerialNumber,
                m.GeneratorId,
                m.Generator == null ? null : m.Generator.Name,
                m.FuelTankId,
                m.FuelTank == null ? null : m.FuelTank.Name,
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
            .Include(m => m.Station)
            .Include(m => m.SubStation)
            .Include(m => m.Generator)
            .Include(m => m.FuelTank)
            .Include(m => m.MeterTypeLookup)
            .Where(m => m.Id == id)
            .Select(m => new MeterDetailDto(
                m.Id,
                m.Name,
                m.Code,
                m.StationId,
                m.Station == null ? null : m.Station.Name,
                m.SubStationId,
                m.SubStation == null ? null : m.SubStation.Name,
                m.MeterType,
                m.MeterTypeLookup != null ? m.MeterTypeLookup.Name : null,
                m.SerialNumber,
                m.GeneratorId,
                m.Generator == null ? null : m.Generator.Name,
                m.FuelTankId,
                m.FuelTank == null ? null : m.FuelTank.Name,
                m.HoursReading,
                m.EnergyReading,
                m.CreatedAt,
                m.UpdatedAt,
                m.IsActive))
            .FirstOrDefaultAsync();
    }
}
