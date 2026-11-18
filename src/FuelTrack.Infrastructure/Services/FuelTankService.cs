using FuelTrack.Application.DTOs.FuelTanks;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class FuelTankService : IFuelTankService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;
    private readonly ICommercialStationModeService _commercialMode;

    public FuelTankService(
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

    public async Task<FuelTankDetailDto> CreateAsync(CreateFuelTankDto dto, int userId)
    {
        var isCommercial = await _commercialMode.IsCommercialModeAsync();
        var commercialScope = isCommercial ? await _commercialMode.GetScopeAsync() : null;
        if (isCommercial && commercialScope is null)
            throw new InvalidOperationException("Commercial mode has no active technical station scope.");

        var subStationId = commercialScope?.SubStationId ?? dto.SubStationId;
        var subStation = await _db.SubStations.FindAsync(subStationId)
            ?? throw new KeyNotFoundException($"SubStation {subStationId} not found.");

        if (dto.TankRole == TankRole.Daily && !dto.GeneratorId.HasValue)
        {
            throw new ArgumentException("Daily tank must be associated with a generator.");
        }

#pragma warning disable CS0618
        if (dto.Shape == TankShape.HorizontalCylinderWithConicalEnds)
        {
            throw new ArgumentException("Horizontal tanks with conical ends (Shape = 1) are not supported. Commercial and enterprise tanks must use vertical cylinder with conical bottom.");
        }
#pragma warning restore CS0618

        var shape = dto.Shape ?? (isCommercial ? TankShape.VerticalCylinderWithConicalBottom : null);

        var tank = new FuelTank
        {
            SubStationId = subStationId,
            FuelLookupId = dto.FuelLookupId,
            Name = dto.Name,
            Code = dto.Code,
            CapacityLiters = dto.CapacityLiters,
            MinAlertLiters = dto.MinAlertLiters,
            DeadStockLiters = dto.DeadStockLiters,
            ActualStockLiters = dto.ActualStockLiters,
            Notes = dto.Notes,
            IsActive = dto.IsActive,
            TankRole = dto.TankRole,
            GeneratorId = dto.GeneratorId,
            ParentTankId = dto.ParentTankId,
            Shape = shape,
            DiameterCm = dto.DiameterCm,
            CylinderHeightCm = dto.CylinderHeightCm,
            ConeHeightCm = dto.ConeHeightCm,
            MultiplicationFactor = dto.MultiplicationFactor,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.FuelTanks.Add(tank);
        await _db.SaveChangesAsync();

        if (tank.GeneratorId.HasValue)
            await _db.Entry(tank).Reference(t => t.Generator).LoadAsync();
        if (tank.ParentTankId.HasValue)
            await _db.Entry(tank).Reference(t => t.ParentTank).LoadAsync();

        return MapToDetail(tank, subStation, null, (false, null, null));
    }

    public async Task<FuelTankDetailDto> UpdateAsync(int id, UpdateFuelTankDto dto, int userId)
    {
        var tank = await _db.FuelTanks
            .Include(ft => ft.SubStation)
            .FirstOrDefaultAsync(ft => ft.Id == id && ft.DeletedAt == null)
            ?? throw new KeyNotFoundException($"FuelTank {id} not found.");

        var isCommercial = await _commercialMode.IsCommercialModeAsync();
        var commercialScope = isCommercial ? await _commercialMode.GetScopeAsync() : null;
        if (isCommercial && commercialScope is null)
            throw new InvalidOperationException("Commercial mode has no active technical station scope.");
        if (commercialScope is not null && tank.SubStationId != commercialScope.SubStationId)
            throw new KeyNotFoundException($"FuelTank {id} not found.");

        if (dto.Name is not null) tank.Name = dto.Name;
        if (dto.Code is not null) tank.Code = dto.Code;
        if (dto.CapacityLiters.HasValue) tank.CapacityLiters = dto.CapacityLiters.Value;
        if (dto.MinAlertLiters.HasValue) tank.MinAlertLiters = dto.MinAlertLiters;
        if (dto.DeadStockLiters.HasValue)
        {
            tank.DeadStockLiters = dto.DeadStockLiters;
            tank.DeadStockLevelCm = null;
            tank.DeadStockInputUnit = "litres";
        }
        if (dto.ActualStockLiters.HasValue) tank.ActualStockLiters = dto.ActualStockLiters;
        if (dto.FuelLookupId.HasValue) tank.FuelLookupId = dto.FuelLookupId.Value;
        if (!isCommercial && dto.SubStationId.HasValue)
        {
            var subStation = await _db.SubStations.FindAsync(dto.SubStationId.Value)
                ?? throw new KeyNotFoundException($"SubStation {dto.SubStationId.Value} not found.");
            tank.SubStationId = dto.SubStationId.Value;
        }
        if (dto.Notes is not null) tank.Notes = dto.Notes;
        if (dto.IsActive.HasValue) tank.IsActive = dto.IsActive.Value;
        if (dto.TankRole.HasValue) tank.TankRole = dto.TankRole.Value;
        if (dto.GeneratorId.HasValue) tank.GeneratorId = dto.GeneratorId.Value;
        if (dto.ParentTankId.HasValue) tank.ParentTankId = dto.ParentTankId.Value;
#pragma warning disable CS0618
        if (dto.Shape == TankShape.HorizontalCylinderWithConicalEnds)
        {
            throw new ArgumentException("Horizontal tanks with conical ends (Shape = 1) are not supported. Commercial and enterprise tanks must use vertical cylinder with conical bottom.");
        }
#pragma warning restore CS0618
        if (dto.Shape.HasValue) tank.Shape = dto.Shape.Value;
        if (dto.DiameterCm.HasValue) tank.DiameterCm = dto.DiameterCm.Value;
        if (dto.CylinderHeightCm.HasValue) tank.CylinderHeightCm = dto.CylinderHeightCm.Value;
        if (dto.ConeHeightCm.HasValue) tank.ConeHeightCm = dto.ConeHeightCm.Value;
        if (dto.MultiplicationFactor.HasValue) tank.MultiplicationFactor = dto.MultiplicationFactor.Value;
        tank.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        if (tank.GeneratorId.HasValue && tank.Generator == null)
            await _db.Entry(tank).Reference(t => t.Generator).LoadAsync();
        if (tank.ParentTankId.HasValue && tank.ParentTank == null)
            await _db.Entry(tank).Reference(t => t.ParentTank).LoadAsync();

        var fuelLookup = await _db.Lookups.FindAsync(tank.FuelLookupId);
        var meterDict = await GetLatestMeterReadingsAsync(new[] { tank.Id });
        meterDict.TryGetValue(tank.SubStationId, out var meterInfo);
        return MapToDetail(tank, tank.SubStation, fuelLookup, meterInfo);
    }

    public async Task<FuelTankDetailDto?> GetByIdAsync(int id)
    {
        var commercialScope = await GetCommercialScopeAsync();
        if (commercialScope is not null)
        {
            var commercialTank = await _db.FuelTanks
                .AsNoTracking()
                .Include(ft => ft.SubStation)
                .Include(ft => ft.FuelLookup)
                .Include(ft => ft.Generator)
                .Include(ft => ft.ParentTank)
                .FirstOrDefaultAsync(ft => ft.Id == id
                    && ft.DeletedAt == null
                    && ft.SubStationId == commercialScope.SubStationId);
            if (commercialTank is null) return null;

            var commercialMeterDict = await GetLatestMeterReadingsAsync(new[] { id });
            commercialMeterDict.TryGetValue(commercialTank.SubStationId, out var commercialMeterInfo);
            return MapToDetail(commercialTank, commercialTank.SubStation, commercialTank.FuelLookup, commercialMeterInfo);
        }

        if (commercialScope is null && !_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return null;
                var tank = await _db.FuelTanks
                     .AsNoTracking()
                     .Include(ft => ft.SubStation)
                     .Include(ft => ft.FuelLookup)
                     .Include(ft => ft.Generator)
                     .Include(ft => ft.ParentTank)
                     .FirstOrDefaultAsync(ft => ft.Id == id && ft.DeletedAt == null && accessibleIds.Contains(ft.SubStationId));
                if (tank is null) return null;

                var meterDict = await GetLatestMeterReadingsAsync(new[] { id });

                meterDict.TryGetValue(tank.SubStationId, out var meterInfo);

                return MapToDetail(tank, tank.SubStation, tank.FuelLookup, meterInfo);
            }
        }

        var originalTank = await _db.FuelTanks
            .AsNoTracking()
            .Include(ft => ft.SubStation)
            .Include(ft => ft.FuelLookup)
            .Include(ft => ft.Generator)
            .Include(ft => ft.ParentTank)
            .FirstOrDefaultAsync(ft => ft.Id == id && ft.DeletedAt == null);

        if (originalTank is null) return null;

        var originalMeterDict = await GetLatestMeterReadingsAsync(new[] { id });
        originalMeterDict.TryGetValue(originalTank.SubStationId, out var originalMeterInfo);

        return MapToDetail(originalTank, originalTank.SubStation, originalTank.FuelLookup, originalMeterInfo);
    }

    public async Task<List<FuelTankListDto>> GetAllAsync()
    {
        var commercialScope = await GetCommercialScopeAsync();
        if (commercialScope is not null)
        {
            var commercialTanks = await _db.FuelTanks
                .AsNoTracking()
                .Include(ft => ft.SubStation)
                .Include(ft => ft.FuelLookup)
                .Include(ft => ft.Generator)
                .Include(ft => ft.ParentTank)
                .Where(ft => ft.DeletedAt == null && ft.SubStationId == commercialScope.SubStationId)
                .OrderBy(ft => ft.Name)
                .ToListAsync();

            return await MapToListAsync(commercialTanks);
        }

        if (commercialScope is null && !_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<FuelTankListDto>();
                var tanks = await _db.FuelTanks
                    .AsNoTracking()
                    .Include(ft => ft.SubStation)
                    .Include(ft => ft.FuelLookup)
                    .Include(ft => ft.Generator)
                    .Include(ft => ft.ParentTank)
                    .Where(ft => ft.DeletedAt == null && accessibleIds.Contains(ft.SubStationId))
                    .OrderBy(ft => ft.SubStation.Name)
                    .ThenBy(ft => ft.Name)
                    .ToListAsync();

                return await MapToListAsync(tanks);
            }
        }

        var allTanks = await _db.FuelTanks
            .AsNoTracking()
            .Include(ft => ft.SubStation)
            .Include(ft => ft.FuelLookup)
            .Include(ft => ft.Generator)
            .Include(ft => ft.ParentTank)
            .Where(ft => ft.DeletedAt == null)
            .OrderBy(ft => ft.SubStation.Name)
            .ThenBy(ft => ft.Name)
            .ToListAsync();

        return await MapToListAsync(allTanks);
    }

    public async Task<List<FuelTankListDto>> GetBySubStationAsync(int subStationId)
    {
        var commercialScope = await GetCommercialScopeAsync();
        if (commercialScope is not null)
        {
            subStationId = commercialScope.SubStationId;
        }

        if (commercialScope is null && !_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
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
            .Include(ft => ft.Generator)
            .Include(ft => ft.ParentTank)
            .Where(ft => ft.DeletedAt == null && ft.SubStationId == subStationId)
            .ToListAsync();

        return await MapToListAsync(tanks);
    }

    public async Task DeleteAsync(int id)
    {
        var tank = await _db.FuelTanks.FindAsync(id)
            ?? throw new KeyNotFoundException($"FuelTank {id} not found.");

        var commercialScope = await GetCommercialScopeAsync();
        if (commercialScope is not null && tank.SubStationId != commercialScope.SubStationId)
            throw new KeyNotFoundException($"FuelTank {id} not found.");

        tank.IsActive = false;
        tank.DeletedAt = DateTime.UtcNow;
        tank.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static FuelTankDetailDto MapToDetail(FuelTank tank, SubStation subStation, Lookup? fuelLookup, (bool HasMeter, double? MeterReading, double? MeterReadingActual) meterInfo)
    {
        return new FuelTankDetailDto(
            tank.Id, tank.SubStationId, subStation.Name,
            tank.FuelLookupId, fuelLookup?.Name ?? "",
            tank.Name, tank.Code, tank.CapacityLiters,
            tank.MinAlertLiters,
            (double?)TankVolumeService.DeadStockLitres(tank),
            tank.ActualStockLiters,
            meterInfo.HasMeter, meterInfo.MeterReading, meterInfo.MeterReadingActual,
            tank.Notes,
            tank.IsActive,
            tank.CreatedAt, tank.UpdatedAt,
            tank.DeadStockLevelCm, tank.DeadStockInputUnit,
            tank.DiameterCm, tank.CylinderHeightCm, tank.ConeHeightCm, tank.MultiplicationFactor,
            tank.TankRole, tank.GeneratorId, tank.Generator?.Name, tank.ParentTankId, tank.ParentTank?.Name, tank.Shape);
    }

    private async Task<CommercialStationScope?> GetCommercialScopeAsync()
    {
        if (!await _commercialMode.IsCommercialModeAsync())
            return null;

        return await _commercialMode.GetScopeAsync()
            ?? throw new InvalidOperationException("Commercial mode has no active technical station scope.");
    }

    private async Task<List<FuelTankListDto>> MapToListAsync(List<FuelTank> tanks)
    {
        var meterDict = await GetLatestMeterReadingsAsync(tanks.Select(t => t.Id).ToArray());
        return tanks.Select(ft =>
        {
            meterDict.TryGetValue(ft.SubStationId, out var meterInfo);
            return new FuelTankListDto(
                ft.Id, ft.SubStationId, ft.SubStation.Name,
                ft.FuelLookupId, ft.FuelLookup?.Name,
                ft.Name, ft.Code, ft.CapacityLiters,
                ft.MinAlertLiters, (double?)TankVolumeService.DeadStockLitres(ft), ft.ActualStockLiters,
                meterInfo.HasMeter, meterInfo.MeterReading, meterInfo.MeterReadingActual,
                ft.IsActive, ft.DeadStockLevelCm, ft.DeadStockInputUnit,
                ft.DiameterCm, ft.CylinderHeightCm, ft.ConeHeightCm, ft.MultiplicationFactor,
                ft.TankRole, ft.GeneratorId, ft.Generator?.Name, ft.ParentTankId, ft.Shape);
        }).ToList();
    }

    private async Task<Dictionary<int, (bool HasMeter, double? MeterReading, double? MeterReadingActual)>> GetLatestMeterReadingsAsync(int[] tankIds)
    {
        if (tankIds.Length == 0)
            return new Dictionary<int, (bool, double?, double?)>();

        var subStationIds = await _db.FuelTanks
            .Where(t => tankIds.Contains(t.Id))
            .Select(t => t.SubStationId)
            .Distinct()
            .ToListAsync();

        return await _db.Meters
            .Where(m => m.SubStationId.HasValue && subStationIds.Contains(m.SubStationId.Value))
            .GroupBy(m => m.SubStationId!.Value)
            .Select(g => new
            {
                SubStationId = g.Key,
                Latest = g.OrderByDescending(m => m.UpdatedAt).First()
            })
            .ToDictionaryAsync(
                x => x.SubStationId,
                x => (true, x.Latest.EnergyReading, x.Latest.HoursReading));
    }
}
