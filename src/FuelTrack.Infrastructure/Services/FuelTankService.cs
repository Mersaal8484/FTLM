using System.Security.Claims;
using FuelTrack.Application.DTOs.FuelTanks;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class FuelTankService : IFuelTankService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FuelTankService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, IHttpContextAccessor httpContextAccessor)
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

    public async Task<FuelTankDetailDto> CreateAsync(CreateFuelTankDto dto, int userId)
    {
        var subStation = await _db.SubStations.FindAsync(dto.SubStationId)
            ?? throw new KeyNotFoundException($"SubStation {dto.SubStationId} not found.");

        var tank = new FuelTank
        {
            SubStationId = dto.SubStationId,
            FuelLookupId = dto.FuelLookupId,
            Name = dto.Name,
            Code = dto.Code,
            CapacityLiters = dto.CapacityLiters,
            MinAlertLiters = dto.MinAlertLiters,
            DeadStockLiters = dto.DeadStockLiters,
            ActualStockLiters = dto.ActualStockLiters,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.FuelTanks.Add(tank);
        await _db.SaveChangesAsync();

        return MapToDetail(tank, subStation, null, (false, null, null));
    }

    public async Task<FuelTankDetailDto> UpdateAsync(int id, UpdateFuelTankDto dto, int userId)
    {
        var tank = await _db.FuelTanks
            .Include(ft => ft.SubStation)
            .FirstOrDefaultAsync(ft => ft.Id == id)
            ?? throw new KeyNotFoundException($"FuelTank {id} not found.");

        if (dto.Name is not null) tank.Name = dto.Name;
        if (dto.Code is not null) tank.Code = dto.Code;
        if (dto.CapacityLiters.HasValue) tank.CapacityLiters = dto.CapacityLiters.Value;
        if (dto.MinAlertLiters.HasValue) tank.MinAlertLiters = dto.MinAlertLiters;
        if (dto.DeadStockLiters.HasValue) tank.DeadStockLiters = dto.DeadStockLiters;
        if (dto.ActualStockLiters.HasValue) tank.ActualStockLiters = dto.ActualStockLiters;
        if (dto.FuelLookupId.HasValue) tank.FuelLookupId = dto.FuelLookupId.Value;
        if (dto.SubStationId.HasValue)
        {
            var subStation = await _db.SubStations.FindAsync(dto.SubStationId.Value)
                ?? throw new KeyNotFoundException($"SubStation {dto.SubStationId.Value} not found.");
            tank.SubStationId = dto.SubStationId.Value;
        }
        if (dto.Notes is not null) tank.Notes = dto.Notes;
        tank.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var fuelLookup = await _db.Lookups.FindAsync(tank.FuelLookupId);
        var meterDict = await GetLatestMeterReadingsAsync(new[] { tank.Id });
        meterDict.TryGetValue(tank.Id, out var meterInfo);
        return MapToDetail(tank, tank.SubStation, fuelLookup, meterInfo);
    }

    public async Task<FuelTankDetailDto?> GetByIdAsync(int id)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return null;
                var tank = await _db.FuelTanks
                    .AsNoTracking()
                    .Include(ft => ft.SubStation)
                    .Include(ft => ft.FuelLookup)
                    .FirstOrDefaultAsync(ft => ft.Id == id && accessibleIds.Contains(ft.SubStationId));
                if (tank is null) return null;

                var meterDict = await GetLatestMeterReadingsAsync(new[] { id });

                meterDict.TryGetValue(id, out var meterInfo);

                return new FuelTankDetailDto(
                    tank.Id, tank.SubStationId, tank.SubStation.Name,
                    tank.FuelLookupId, tank.FuelLookup.Name,
                    tank.Name, tank.Code, tank.CapacityLiters,
                    tank.MinAlertLiters,
                    tank.DeadStockLiters, tank.ActualStockLiters,
                    meterInfo.HasMeter, meterInfo.MeterReading, meterInfo.MeterReadingActual,
                    tank.Notes,
                    tank.CreatedAt, tank.UpdatedAt);
            }
        }

        var originalTank = await _db.FuelTanks
            .AsNoTracking()
            .Include(ft => ft.SubStation)
            .Include(ft => ft.FuelLookup)
            .FirstOrDefaultAsync(ft => ft.Id == id);

        if (originalTank is null) return null;

        var originalMeterDict = await GetLatestMeterReadingsAsync(new[] { id });
        originalMeterDict.TryGetValue(id, out var originalMeterInfo);

        return new FuelTankDetailDto(
            originalTank.Id, originalTank.SubStationId, originalTank.SubStation.Name,
            originalTank.FuelLookupId, originalTank.FuelLookup.Name,
            originalTank.Name, originalTank.Code, originalTank.CapacityLiters,
            originalTank.MinAlertLiters,
            originalTank.DeadStockLiters, originalTank.ActualStockLiters,
            originalMeterInfo.HasMeter, originalMeterInfo.MeterReading, originalMeterInfo.MeterReadingActual,
            originalTank.Notes,
            originalTank.CreatedAt, originalTank.UpdatedAt);
    }

    public async Task<List<FuelTankListDto>> GetAllAsync()
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<FuelTankListDto>();
                var tanks = await _db.FuelTanks
                    .AsNoTracking()
                    .Include(ft => ft.SubStation)
                    .Include(ft => ft.FuelLookup)
                    .Where(ft => accessibleIds.Contains(ft.SubStationId))
                    .OrderBy(ft => ft.SubStation.Name)
                    .ThenBy(ft => ft.Name)
                    .ToListAsync();

                var tankIds = tanks.Select(t => t.Id).ToArray();
                var meterDict = await GetLatestMeterReadingsAsync(tankIds);

                return tanks.Select(ft =>
                {
                    meterDict.TryGetValue(ft.Id, out var m);
                    return new FuelTankListDto(
                        ft.Id, ft.SubStationId, ft.SubStation.Name,
                        ft.FuelLookupId, ft.FuelLookup?.Name,
                        ft.Name, ft.Code, ft.CapacityLiters,
                        ft.MinAlertLiters, ft.DeadStockLiters, ft.ActualStockLiters,
                        m.HasMeter, m.MeterReading, m.MeterReadingActual);
                }).ToList();
            }
        }

        var allTanks = await _db.FuelTanks
            .AsNoTracking()
            .Include(ft => ft.SubStation)
            .Include(ft => ft.FuelLookup)
            .OrderBy(ft => ft.SubStation.Name)
            .ThenBy(ft => ft.Name)
            .ToListAsync();

        var allTankIds = allTanks.Select(t => t.Id).ToArray();
        var allMeterDict = await GetLatestMeterReadingsAsync(allTankIds);

        return allTanks.Select(ft =>
        {
            allMeterDict.TryGetValue(ft.Id, out var m);
            return new FuelTankListDto(
                ft.Id, ft.SubStationId, ft.SubStation.Name,
                ft.FuelLookupId, ft.FuelLookup?.Name,
                ft.Name, ft.Code, ft.CapacityLiters,
                ft.MinAlertLiters, ft.DeadStockLiters, ft.ActualStockLiters,
                m.HasMeter, m.MeterReading, m.MeterReadingActual);
        }).ToList();
    }

    public async Task<List<FuelTankListDto>> GetBySubStationAsync(int subStationId)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0 || !accessibleIds.Contains(subStationId))
                    return new List<FuelTankListDto>();
            }
        }

        var tanks = await _db.FuelTanks
            .AsNoTracking()
            .Include(ft => ft.SubStation)
            .Include(ft => ft.FuelLookup)
            .Where(ft => ft.SubStationId == subStationId)
            .ToListAsync();

        var tankIds = tanks.Select(t => t.Id).ToArray();
        var meterDict = await GetLatestMeterReadingsAsync(tankIds);

        return tanks.Select(ft =>
        {
            meterDict.TryGetValue(ft.Id, out var m);
            return new FuelTankListDto(
                ft.Id, ft.SubStationId, ft.SubStation.Name,
                ft.FuelLookupId, ft.FuelLookup?.Name,
                ft.Name, ft.Code, ft.CapacityLiters,
                ft.MinAlertLiters, ft.DeadStockLiters, ft.ActualStockLiters,
                m.HasMeter, m.MeterReading, m.MeterReadingActual);
        }).ToList();
    }

    public async Task DeleteAsync(int id)
    {
        var tank = await _db.FuelTanks.FindAsync(id)
            ?? throw new KeyNotFoundException($"FuelTank {id} not found.");
        _db.FuelTanks.Remove(tank);
        await _db.SaveChangesAsync();
    }

    private static FuelTankDetailDto MapToDetail(FuelTank tank, SubStation subStation, Lookup? fuelLookup, (bool HasMeter, double? MeterReading, double? MeterReadingActual) meterInfo)
    {
        return new FuelTankDetailDto(
            tank.Id, tank.SubStationId, subStation.Name,
            tank.FuelLookupId, fuelLookup?.Name ?? "",
            tank.Name, tank.Code, tank.CapacityLiters,
            tank.MinAlertLiters,
            tank.DeadStockLiters,
            tank.ActualStockLiters,
            meterInfo.HasMeter, meterInfo.MeterReading, meterInfo.MeterReadingActual,
            tank.Notes,
            tank.CreatedAt, tank.UpdatedAt);
    }

    private async Task<Dictionary<int, (bool HasMeter, double? MeterReading, double? MeterReadingActual)>> GetLatestMeterReadingsAsync(int[] tankIds)
    {
        if (tankIds.Length == 0)
            return new Dictionary<int, (bool, double?, double?)>();

        return await _db.Meters
            .Where(m => m.FuelTankId.HasValue && tankIds.Contains(m.FuelTankId.Value))
            .GroupBy(m => m.FuelTankId!.Value)
            .Select(g => new
            {
                TankId = g.Key,
                Latest = g.OrderByDescending(m => m.UpdatedAt).First()
            })
            .ToDictionaryAsync(
                x => x.TankId,
                x => (true, x.Latest.EnergyReading, x.Latest.HoursReading));
    }
}
