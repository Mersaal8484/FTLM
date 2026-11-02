using FuelTrack.Application.DTOs.Efficiency;
using FuelTrack.Application.DTOs.Inventory;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FuelTrack.Infrastructure.Services;

public class PeriodicInventoryService : IPeriodicInventoryService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;

    public PeriodicInventoryService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, ICurrentUserService currentUser)
    {
        _db = db;
        _scopeFilter = scopeFilter;
        _currentUser = currentUser;
    }

    public async Task<PeriodicInventoryDto> CreateAsync(CreatePeriodicInventoryDto dto, int userId)
    {
        if (!_currentUser.HasAnyAdminRole && !await _scopeFilter.HasAccessToSubStationAsync(userId, dto.SubStationId))
            throw new UnauthorizedAccessException("You do not have access to this substation.");

        var subStation = await _db.SubStations.FindAsync(dto.SubStationId)
            ?? throw new KeyNotFoundException($"SubStation {dto.SubStationId} not found.");

        await ValidateFuelCapacityAsync(dto.SubStationId, dto.FuelReadings);

        var inventory = new PeriodicInventory
        {
            SubStationId = dto.SubStationId,
            Year = dto.Year,
            Month = dto.Month,
            PeriodTypeLookupId = dto.PeriodTypeLookupId,
            InventoryDate = dto.InventoryDate,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
        };

        _db.PeriodicInventories.Add(inventory);

        if (_db.Database.IsRelational())
        {
            var executionStrategy = _db.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    await _db.SaveChangesAsync();
                    var fuelResults = await AddFuelReadingsAsync(inventory.Id, dto.SubStationId, dto.Year, dto.Month, dto.FuelReadings);
                    var oilResults = await AddOilReadingsAsync(inventory.Id, dto.OilReadings);
                    await transaction.CommitAsync();
                    return BuildDto(inventory, fuelResults, oilResults);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        await _db.SaveChangesAsync();
        var fuelResult = await AddFuelReadingsAsync(inventory.Id, dto.SubStationId, dto.Year, dto.Month, dto.FuelReadings);
        var oilResult = await AddOilReadingsAsync(inventory.Id, dto.OilReadings);
        return BuildDto(inventory, fuelResult, oilResult);
    }

    public async Task<PeriodicInventoryDto?> GetByIdAsync(int id)
    {
        return await _db.PeriodicInventories
            .Include(pi => pi.SubStation)
            .Include(pi => pi.PeriodTypeLookup)
            .Include(pi => pi.ApprovedBy)
            .Include(pi => pi.FuelReadings).ThenInclude(fr => fr.FuelLookup)
            .Include(pi => pi.OilReadings).ThenInclude(or => or.OilLookup)
            .Include(pi => pi.OilReadings).ThenInclude(or => or.Oil).ThenInclude(o => o!.OilLookup)
            .Where(pi => pi.Id == id)
            .Select(pi => new PeriodicInventoryDto(
                pi.Id, pi.SubStationId, pi.SubStation.Name,
                pi.Year, pi.Month, pi.PeriodTypeLookupId,
                pi.PeriodTypeLookup.Name,
                pi.InventoryDate,
                pi.Notes,
                pi.IsApproved,
                pi.ApprovedById, pi.ApprovedBy != null ? pi.ApprovedBy.FullName : null,
                pi.ApprovedAt,
                pi.CreatedBy, pi.UpdatedBy,
                pi.CreatedAt, pi.UpdatedAt,
                pi.FuelReadings.Select(fr => new FuelReadingResultDto(
                    fr.Id,
                    fr.FuelLookupId, fr.FuelLookup.Name,
                    fr.ActualStockLiters)).ToList(),
                pi.OilReadings.Select(or => new OilReadingResultDto(
                    or.Id, or.OilId, or.OilId != null ? or.Oil!.OilLookup!.Name : null,
                    or.OilLookupId, or.OilLookup.Name,
                    or.ActualStockLiters)).ToList()))
            .FirstOrDefaultAsync();
    }

    public async Task<List<PeriodicInventoryListDto>> GetBySubStationAsync(int subStationId)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (!accessibleIds.Contains(subStationId))
                    return new List<PeriodicInventoryListDto>();
            }
        }

        return await _db.PeriodicInventories
            .Include(pi => pi.SubStation)
            .Include(pi => pi.PeriodTypeLookup)
            .Where(pi => pi.SubStationId == subStationId)
            .OrderByDescending(pi => pi.InventoryDate)
            .Select(pi => new PeriodicInventoryListDto(
                pi.Id, pi.SubStationId, pi.SubStation.Name,
                pi.Year, pi.Month, pi.PeriodTypeLookup.Name,
                pi.InventoryDate,
                pi.Notes, pi.CreatedAt))
            .ToListAsync();
    }

    public async Task<PeriodicInventoryDto> UpdateAsync(int id, CreatePeriodicInventoryDto dto, int userId)
    {
        if (!_currentUser.HasAnyAdminRole && !await _scopeFilter.HasAccessToSubStationAsync(userId, dto.SubStationId))
            throw new UnauthorizedAccessException("You do not have access to this substation.");

        var inventory = await _db.PeriodicInventories
            .Include(pi => pi.FuelReadings)
            .Include(pi => pi.OilReadings)
            .FirstOrDefaultAsync(pi => pi.Id == id && pi.DeletedAt == null)
            ?? throw new KeyNotFoundException($"PeriodicInventory {id} not found");

        if (inventory.IsApproved && dto.IsApproved != false)
            throw new InvalidOperationException("Cannot update an approved periodic inventory.");

        if (dto.IsApproved == true && !inventory.IsApproved)
        {
            inventory.IsApproved = true;
            inventory.ApprovedById = userId;
            inventory.ApprovedAt = DateTime.UtcNow;
        }
        else if (dto.IsApproved == false)
        {
            inventory.IsApproved = false;
            inventory.ApprovedById = null;
            inventory.ApprovedAt = null;
        }

        await ValidateFuelCapacityAsync(dto.SubStationId, dto.FuelReadings);

        inventory.SubStationId = dto.SubStationId;
        inventory.Year = dto.Year;
        inventory.Month = dto.Month;
        inventory.PeriodTypeLookupId = dto.PeriodTypeLookupId;
        inventory.InventoryDate = dto.InventoryDate;
        inventory.Notes = dto.Notes;
        inventory.UpdatedAt = DateTime.UtcNow;
        inventory.UpdatedBy = userId;

        _db.PeriodicInventoryFuels.RemoveRange(inventory.FuelReadings);
        _db.PeriodicInventoryOils.RemoveRange(inventory.OilReadings);

        if (_db.Database.IsRelational())
        {
            var executionStrategy = _db.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    await _db.SaveChangesAsync();
                    var fuelResults = await AddFuelReadingsAsync(inventory.Id, dto.SubStationId, dto.Year, dto.Month, dto.FuelReadings);
                    var oilResults = await AddOilReadingsAsync(inventory.Id, dto.OilReadings);
                    await transaction.CommitAsync();
                    return BuildDto(inventory, fuelResults, oilResults);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        await _db.SaveChangesAsync();
        var fuelResult = await AddFuelReadingsAsync(inventory.Id, dto.SubStationId, dto.Year, dto.Month, dto.FuelReadings);
        var oilResult = await AddOilReadingsAsync(inventory.Id, dto.OilReadings);
        return BuildDto(inventory, fuelResult, oilResult);
    }

    public async Task ApproveAsync(int id, int approvedById)
    {
        var inventory = await _db.PeriodicInventories.FindAsync(id)
            ?? throw new KeyNotFoundException($"PeriodicInventory {id} not found");

        if (inventory.IsApproved)
            throw new InvalidOperationException("PeriodicInventory is already approved.");

        inventory.IsApproved = true;
        inventory.ApprovedById = approvedById;
        inventory.ApprovedAt = DateTime.UtcNow;
        inventory.UpdatedAt = DateTime.UtcNow;
        inventory.UpdatedBy = approvedById;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var inventory = await _db.PeriodicInventories.FindAsync(id)
            ?? throw new KeyNotFoundException($"PeriodicInventory {id} not found");

        inventory.DeletedAt = DateTime.UtcNow;
        inventory.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<EfficiencyResultDto> CalculateEfficiencyAsync(int id)
    {
        var inventory = await _db.PeriodicInventories
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null)
            ?? throw new KeyNotFoundException($"PeriodicInventory {id} not found");

        var startDate = new DateTime(inventory.Year, inventory.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var readings = await _db.DailyReadings
            .AsNoTracking()
            .Where(r => r.ReadingDate >= startDate
                     && r.ReadingDate <= endDate
                     && r.Generator.SubStationId == inventory.SubStationId
                     && r.DeletedAt == null)
            .ToListAsync();

        if (!readings.Any())
            return new EfficiencyResultDto(0, 0, 0);

        var totalEnergy = readings.Sum(r => r.CurrentEnergyKWh - r.PrevEnergyKWh);
        var totalFuel = (double)readings.Sum(r =>
            (decimal)(r.CurrentEnergyKWh - r.PrevEnergyKWh) * r.StandardFuelRate);
        var efficiency = totalFuel > 0 ? Math.Round(totalEnergy / totalFuel, 4) : 0;

        return new EfficiencyResultDto(totalEnergy, totalFuel, efficiency);
    }

    public async Task<List<PeriodicInventoryListDto>> GetAllAsync(string? status = null)
    {
        var query = _db.PeriodicInventories
            .AsNoTracking()
            .Include(pi => pi.SubStation)
            .Include(pi => pi.PeriodTypeLookup)
            .AsQueryable();

        if (status == "pending")
        {
            query = query.Where(pi => !pi.IsApproved);
        }

        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                query = query.Where(pi => accessibleIds.Contains(pi.SubStationId));
            }
        }

        return await query
            .OrderByDescending(pi => pi.InventoryDate)
            .Select(pi => new PeriodicInventoryListDto(
                pi.Id, pi.SubStationId, pi.SubStation.Name,
                pi.Year, pi.Month, pi.PeriodTypeLookup.Name,
                pi.InventoryDate,
                pi.Notes, pi.CreatedAt))
            .ToListAsync();
    }

    private async Task<List<FuelReadingResultDto>> AddFuelReadingsAsync(int inventoryId, int subStationId, int year, int month, List<FuelReadingDto>? readings)
    {
        var results = new List<FuelReadingResultDto>();
        if (readings is null || readings.Count == 0) return results;

        var fuelLookupIds = readings.Select(r => r.FuelLookupId).Distinct().ToList();
        var fuelLookupNames = await _db.Lookups
            .Where(l => fuelLookupIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.Name);

        var entities = readings.Select(fr => new PeriodicInventoryFuel
        {
            PeriodicInventoryId = inventoryId,
            FuelLookupId = fr.FuelLookupId,
            ActualStockLiters = fr.ActualQtyLiters,
        }).ToList();

        _db.PeriodicInventoryFuels.AddRange(entities);
        await _db.SaveChangesAsync();

        foreach (var entity in entities)
        {
            fuelLookupNames.TryGetValue(entity.FuelLookupId, out var name);
            results.Add(new FuelReadingResultDto(
                entity.Id,
                entity.FuelLookupId, name,
                entity.ActualStockLiters));
        }

        return results;
    }

    private async Task<List<OilReadingResultDto>> AddOilReadingsAsync(int inventoryId, List<OilReadingDto>? readings)
    {
        var results = new List<OilReadingResultDto>();
        if (readings is null || readings.Count == 0) return results;

        var oilLookupIds = readings.Select(r => r.OilLookupId).Distinct().ToList();
        var oilLookupNames = await _db.Lookups
            .Where(l => oilLookupIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.Name);

        var oilIds = readings.Where(r => r.OilId.HasValue).Select(r => r.OilId!.Value).Distinct().ToList();
        var oilNames = oilIds.Count > 0
            ? await _db.Oils.Where(o => oilIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.OilLookup != null ? o.OilLookup!.Name : null)
            : new Dictionary<int, string?>();

        var entities = readings.Select(or => new PeriodicInventoryOil
        {
            PeriodicInventoryId = inventoryId,
            OilLookupId = or.OilLookupId,
            OilId = or.OilId,
            ActualStockLiters = or.ActualQtyLiters,
        }).ToList();

        _db.PeriodicInventoryOils.AddRange(entities);
        await _db.SaveChangesAsync();

        foreach (var entity in entities)
        {
            oilLookupNames.TryGetValue(entity.OilLookupId, out var lookupName);
            oilNames.TryGetValue(entity.OilId ?? 0, out var oilName);
            results.Add(new OilReadingResultDto(
                entity.Id, entity.OilId, oilName,
                entity.OilLookupId, lookupName,
                entity.ActualStockLiters));
        }

        return results;
    }

    private static PeriodicInventoryDto BuildDto(PeriodicInventory inventory, List<FuelReadingResultDto> fuelResults, List<OilReadingResultDto> oilResults)
    {
        return new PeriodicInventoryDto(
            inventory.Id, inventory.SubStationId, inventory.SubStation?.Name,
            inventory.Year, inventory.Month, inventory.PeriodTypeLookupId,
            inventory.PeriodTypeLookup?.Name,
            inventory.InventoryDate,
            inventory.Notes,
            inventory.IsApproved,
            inventory.ApprovedById, null,
            inventory.ApprovedAt,
            inventory.CreatedBy, inventory.UpdatedBy,
            inventory.CreatedAt, inventory.UpdatedAt,
            fuelResults, oilResults);
    }

    private async Task ValidateFuelCapacityAsync(int subStationId, List<FuelReadingDto>? readings)
    {
        if (readings is null || readings.Count == 0) return;

        var fuelLookupIds = readings.Select(r => r.FuelLookupId).Distinct().ToList();

        var tanks = await _db.FuelTanks
            .Where(t => t.SubStationId == subStationId
                     && fuelLookupIds.Contains(t.FuelLookupId)
                     && t.DeletedAt == null)
            .ToListAsync();

        foreach (var reading in readings)
        {
            var matchingTanks = tanks.Where(t => t.FuelLookupId == reading.FuelLookupId).ToList();
            if (matchingTanks.Count == 0) continue;

            var totalCapacity = matchingTanks.Sum(t => t.CapacityLiters);
            if (reading.ActualQtyLiters > totalCapacity)
            {
                var fuelName = (await _db.Lookups.FindAsync(reading.FuelLookupId))?.Name ?? reading.FuelLookupId.ToString();
                throw new InvalidOperationException(
                    $"R4: الكمية الفعلية للوقود \"{fuelName}\" ({reading.ActualQtyLiters:N2} لتر) تتجاوز سعة الخزانات ({totalCapacity:N2} لتر)");
            }
        }
    }
}
