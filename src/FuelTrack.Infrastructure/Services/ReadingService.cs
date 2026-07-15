using FuelTrack.Application;
using FuelTrack.Application.DTOs.Readings;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FuelTrack.Infrastructure.Services;

public class ReadingService : IReadingService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ILogger<ReadingService> _logger;

    public ReadingService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, ILogger<ReadingService> logger)
    {
        _db = db;
        _scopeFilter = scopeFilter;
        _logger = logger;
    }

    private async Task<int> ResolveFuelLookupIdAsync(int provided, int subStationId)
    {
        if (provided > 0 && await _db.Lookups.AnyAsync(l => l.Id == provided))
            return provided;

        // Fall back to a fuel tank configured for the generator's sub-station.
        var fromTank = await _db.FuelTanks
            .Where(ft => ft.SubStationId == subStationId)
            .Select(ft => ft.FuelLookupId)
            .FirstOrDefaultAsync();
        if (fromTank > 0 && await _db.Lookups.AnyAsync(l => l.Id == fromTank))
            return fromTank;

        // Last resort: any lookup in the FuelType category.
        var fuelCategoryId = await _db.LookupCategories
            .Where(c => c.Name == "FuelType")
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
        if (fuelCategoryId > 0)
        {
            var anyFuel = await _db.Lookups
                .Where(l => l.CategoryId == fuelCategoryId)
                .Select(l => l.Id)
                .FirstOrDefaultAsync();
            if (anyFuel > 0)
                return anyFuel;
        }

        throw new InvalidOperationException("A fuel type could not be determined for this reading. Configure a fuel tank or fuel lookup for the sub-station.");
    }

    private async Task<int> ResolveOilLookupIdAsync(int provided, int subStationId)
    {
        if (provided > 0 && await _db.Lookups.AnyAsync(l => l.Id == provided))
            return provided;

        // Fall back to an oil assigned to the generator's sub-station.
        var fromOil = await _db.Oils
            .Where(o => o.SubStationId == subStationId)
            .Select(o => o.OilLookupId)
            .FirstOrDefaultAsync();
        if (fromOil > 0 && await _db.Lookups.AnyAsync(l => l.Id == fromOil))
            return fromOil;

        // Last resort: any lookup in the OilType category.
        var oilCategoryId = await _db.LookupCategories
            .Where(c => c.Name == "OilType")
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
        if (oilCategoryId > 0)
        {
            var anyOil = await _db.Lookups
                .Where(l => l.CategoryId == oilCategoryId)
                .Select(l => l.Id)
                .FirstOrDefaultAsync();
            if (anyOil > 0)
                return anyOil;
        }

        throw new InvalidOperationException("An oil type could not be determined for this reading. Configure an oil for the sub-station or an Oil lookup.");
    }

    private async Task<int> GetLookupIdAsync(string category, string name)
    {
        return await _db.Lookups
            .Where(l => l.Category.Name == category && l.Name == name)
            .Select(l => l.Id)
            .FirstAsync();
    }

    public async Task<ReadingDetailDto> CreateAsync(CreateReadingDto dto, int userId, bool isAdmin = false)
    {
        var generator = await _db.Generators.FindAsync(dto.GeneratorId)
            ?? throw new KeyNotFoundException($"Generator {dto.GeneratorId} not found.");

        // Reject readings for generators without a linked control panel.
        var hasController = await _db.Controllers.AnyAsync(c => c.GeneratorId == dto.GeneratorId);
        if (!hasController)
            throw new InvalidOperationException("Cannot create reading: generator is not linked to an active control panel. Link the generator to a control panel first.");

        if (!isAdmin)
        {
            var hasAccess = await _scopeFilter.HasAccessToSubStationAsync(userId, generator.SubStationId);
            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} attempted to create reading for generator {GeneratorId} without scope access.", userId, dto.GeneratorId);
                throw new UnauthorizedAccessException("You do not have access to this generator's sub-station.");
            }
        }

        var exists = await _db.DailyReadings.AnyAsync(dr =>
            dr.GeneratorId == dto.GeneratorId &&
            dr.ReadingDate == dto.ReadingDate);

        if (exists)
            throw new InvalidOperationException("A reading already exists for this generator and date.");

        // The client may submit 0 / invalid lookup ids (the generator DTO does not
        // always expose them). Resolve a valid fuel/oil lookup so the required FK
        // is satisfied instead of crashing with a 500.
        var fuelLookupId = await ResolveFuelLookupIdAsync(dto.FuelLookupId, generator.SubStationId);
        var oilLookupId = await ResolveOilLookupIdAsync(dto.OilLookupId, generator.SubStationId);

        // Record the control panel (controller) currently linked to the generator
        var controller = await _db.Controllers
            .FirstOrDefaultAsync(c => c.GeneratorId == dto.GeneratorId);

        var draftLookupId = await GetLookupIdAsync("ReadingStatus", "Draft");

        var reading = new DailyReading
        {
            GeneratorId = dto.GeneratorId,
            ControllerId = controller?.Id,
            ReadingDate = dto.ReadingDate,
            PrevRunningHours = dto.PrevRunningHours,
            CurrentRunningHours = dto.CurrentRunningHours,
            PrevEnergyKWh = dto.PrevEnergyKWh,
            CurrentEnergyKWh = dto.CurrentEnergyKWh,
            FuelLookupId = fuelLookupId,
            StandardFuelRate = dto.StandardFuelRate,
            OilLookupId = oilLookupId,
            OilUsedLiters = dto.OilUsedLiters ?? 0,
            ReadingStatusLookupId = draftLookupId,
            Notes = dto.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
        };

        _db.DailyReadings.Add(reading);
        await _db.SaveChangesAsync();

        return await MapToDetailAsync(reading.Id)
            ?? throw new InvalidOperationException("Failed to create reading.");
    }

    public async Task<ReadingDetailDto> UpdateAsync(int id, UpdateReadingDto dto, int userId, bool isAdmin = false)
    {
        var reading = await GetReadingOrThrowAsync(id);

        if (!isAdmin)
        {
            var generator = await _db.Generators.FindAsync(reading.GeneratorId)
                ?? throw new KeyNotFoundException($"Generator {reading.GeneratorId} not found.");
            var hasAccess = await _scopeFilter.HasAccessToSubStationAsync(userId, generator.SubStationId);
            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} attempted to update reading {ReadingId} without scope access.", userId, id);
                throw new UnauthorizedAccessException("You do not have access to this reading's sub-station.");
            }
        }
        var draftLookupId = await GetLookupIdAsync("ReadingStatus", "Draft");
        EnsureStatusOrThrow(reading.ReadingStatusLookupId, draftLookupId, "updated");

        if (dto.PrevRunningHours.HasValue) reading.PrevRunningHours = dto.PrevRunningHours.Value;
        if (dto.CurrentRunningHours.HasValue) reading.CurrentRunningHours = dto.CurrentRunningHours.Value;
        if (dto.PrevEnergyKWh.HasValue) reading.PrevEnergyKWh = dto.PrevEnergyKWh.Value;
        if (dto.CurrentEnergyKWh.HasValue) reading.CurrentEnergyKWh = dto.CurrentEnergyKWh.Value;
        if (dto.StandardFuelRate.HasValue) reading.StandardFuelRate = dto.StandardFuelRate.Value;
        if (dto.OilLookupId.HasValue) reading.OilLookupId = dto.OilLookupId.Value;
        if (dto.OilUsedLiters.HasValue) reading.OilUsedLiters = dto.OilUsedLiters.Value;
        if (dto.Notes is not null) reading.Notes = dto.Notes;

        reading.UpdatedAt = DateTime.UtcNow;
        reading.UpdatedBy = userId;

        await _db.SaveChangesAsync();
        return (await MapToDetailAsync(reading.Id))!;
    }

    public async Task<ReadingDetailDto> ApproveAsync(int id, ApproveReadingDto dto)
    {
        var reading = await _db.DailyReadings
            .Include(dr => dr.Generator)
            .FirstOrDefaultAsync(dr => dr.Id == id)
            ?? throw new KeyNotFoundException($"Reading {id} not found.");

        var draftLookupId = await GetLookupIdAsync("ReadingStatus", "Draft");
        var approvedLookupId = await GetLookupIdAsync("ReadingStatus", "Approved");

        if (reading.ReadingStatusLookupId != draftLookupId)
            throw new InvalidOperationException("Only draft readings can be approved.");

        reading.ReadingStatusLookupId = approvedLookupId;
        reading.UpdatedAt = DateTime.UtcNow;
        reading.UpdatedBy = dto.ApprovedById;

        var generator = reading.Generator;
        var fuelConsumed = CalculateFuelConsumption(reading.CurrentEnergyKWh, reading.PrevEnergyKWh, (double)reading.StandardFuelRate);

        if (fuelConsumed > 0)
        {
            var consumptionLookupId = await GetLookupIdAsync("TransactionType", "Consumption");

            var transaction = new FuelTransaction
            {
                SubStationId = generator.SubStationId,
                FuelLookupId = reading.FuelLookupId,
                TransactionDate = reading.ReadingDate,
                QuantityLiters = fuelConsumed,
                TransactionTypeLookupId = consumptionLookupId,
                Notes = $"Auto-generated from reading approval on {reading.ReadingDate:d}. Generator: {generator.Name}",
                IsApproved = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = dto.ApprovedById,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = dto.ApprovedById,
            };

            _db.FuelTransactions.Add(transaction);
        }

        await _db.SaveChangesAsync();
        return (await MapToDetailAsync(reading.Id))!;
    }

    public async Task<ReadingDetailDto> RejectAsync(int id, RejectReadingDto dto)
    {
        var reading = await GetReadingOrThrowAsync(id);
        var draftLookupId = await GetLookupIdAsync("ReadingStatus", "Draft");
        var rejectedLookupId = await GetLookupIdAsync("ReadingStatus", "Rejected");

        EnsureStatusOrThrow(reading.ReadingStatusLookupId, draftLookupId, "rejected");

        reading.ReadingStatusLookupId = rejectedLookupId;
        reading.RejectionReason = dto.RejectionReason;
        reading.UpdatedAt = DateTime.UtcNow;
        reading.UpdatedBy = dto.RejectedById;

        await _db.SaveChangesAsync();
        return (await MapToDetailAsync(reading.Id))!;
    }

    public async Task<ReadingDetailDto> ReviewAsync(int id, int userId)
    {
        var reading = await GetReadingOrThrowAsync(id);
        var approvedLookupId = await GetLookupIdAsync("ReadingStatus", "Approved");
        var reviewedLookupId = await GetLookupIdAsync("ReadingStatus", "Reviewed");

        EnsureStatusOrThrow(reading.ReadingStatusLookupId, approvedLookupId, "reviewed");

        reading.ReadingStatusLookupId = reviewedLookupId;
        reading.UpdatedAt = DateTime.UtcNow;
        reading.UpdatedBy = userId;

        await _db.SaveChangesAsync();
        return (await MapToDetailAsync(reading.Id))!;
    }

    public async Task DeleteAsync(int id)
    {
        var reading = await GetReadingOrThrowAsync(id);
        var draftLookupId = await GetLookupIdAsync("ReadingStatus", "Draft");
        EnsureStatusOrThrow(reading.ReadingStatusLookupId, draftLookupId, "deleted");

        _db.DailyReadings.Remove(reading);
        await _db.SaveChangesAsync();
    }

    public async Task<ReadingDetailDto?> GetByIdAsync(int id)
    {
        return await MapToDetailAsync(id);
    }

    public async Task<PagedResult<ReadingListDto>> ListAsync(ReadingFilterDto filter, int? userId = null, bool isAdmin = false)
    {
        var query = _db.DailyReadings
            .Include(dr => dr.Generator)
            .Include(dr => dr.FuelLookup)
            .Include(dr => dr.ReadingStatusLookup)
            .AsQueryable();

        if (!isAdmin && userId.HasValue)
        {
            var accessibleSubStationIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
            if (accessibleSubStationIds.Count == 0)
            {
                return new PagedResult<ReadingListDto>
                {
                    Items = new List<ReadingListDto>(),
                    TotalCount = 0,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                };
            }
            var accessibleGeneratorIds = await _db.Generators
                .Where(g => accessibleSubStationIds.Contains(g.SubStationId))
                .Select(g => g.Id)
                .ToListAsync();
            query = query.Where(dr => accessibleGeneratorIds.Contains(dr.GeneratorId));
        }

        if (filter.GeneratorId.HasValue)
            query = query.Where(dr => dr.GeneratorId == filter.GeneratorId.Value);
        if (filter.DateFrom.HasValue)
            query = query.Where(dr => dr.ReadingDate >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue)
            query = query.Where(dr => dr.ReadingDate <= filter.DateTo.Value);
        if (filter.ReadingStatusLookupId.HasValue)
            query = query.Where(dr => dr.ReadingStatusLookupId == filter.ReadingStatusLookupId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(dr => dr.ReadingDate)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(dr => new ReadingListDto(
                dr.Id,
                dr.GeneratorId,
                dr.Generator.Name,
                dr.ReadingDate,
                dr.CurrentRunningHours,
                dr.CurrentEnergyKWh,
                dr.FuelLookup.Name,
                dr.ReadingStatusLookupId,
                dr.ReadingStatusLookup!.Name,
                dr.CreatedAt,
                dr.ControllerId,
                dr.Controller != null ? dr.Controller.Name : null
            ))
            .ToListAsync();

        return new PagedResult<ReadingListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
        };
    }

    public async Task<ReadingConsumptionDto> GetConsumptionAsync(int id)
    {
        var reading = await _db.DailyReadings.FindAsync(id)
            ?? throw new KeyNotFoundException($"Reading {id} not found.");

        var energyDelta = reading.CurrentEnergyKWh - reading.PrevEnergyKWh;
        var runningDelta = reading.CurrentRunningHours - reading.PrevRunningHours;
        var fuelConsumed = CalculateFuelConsumption(reading.CurrentEnergyKWh, reading.PrevEnergyKWh, (double)reading.StandardFuelRate);

        return new ReadingConsumptionDto(
            reading.PrevRunningHours,
            reading.CurrentRunningHours,
            runningDelta,
            reading.PrevEnergyKWh,
            reading.CurrentEnergyKWh,
            energyDelta,
            (double)reading.StandardFuelRate,
            fuelConsumed);
    }

    private async Task<DailyReading> GetReadingOrThrowAsync(int id)
    {
        return await _db.DailyReadings.FindAsync(id)
            ?? throw new KeyNotFoundException($"Reading {id} not found.");
    }

    private static void EnsureStatusOrThrow(int? current, int expected, string action)
    {
        if (current != expected)
            throw new InvalidOperationException("Only draft readings can be " + action + ".");
    }

    private static double CalculateFuelConsumption(double currentKWh, double prevKWh, double standardRate)
    {
        var delta = currentKWh - prevKWh;
        return standardRate > 0 ? delta / standardRate : 0;
    }

    private async Task<ReadingDetailDto?> MapToDetailAsync(int id)
    {
        return await _db.DailyReadings
            .Include(dr => dr.Generator)
            .Include(dr => dr.FuelLookup)
            .Include(dr => dr.OilLookup)
            .Include(dr => dr.ReadingStatusLookup)
            .Include(dr => dr.Controller)
            .Where(dr => dr.Id == id)
            .Select(dr => new ReadingDetailDto(
                dr.Id,
                dr.GeneratorId,
                dr.Generator.Name,
                dr.ReadingDate,
                dr.PrevRunningHours,
                dr.CurrentRunningHours,
                dr.PrevEnergyKWh,
                dr.CurrentEnergyKWh,
                dr.FuelLookupId,
                dr.FuelLookup.Name,
                dr.StandardFuelRate,
                dr.OilLookupId,
                dr.OilLookup != null ? dr.OilLookup.Name : null,
                dr.OilUsedLiters,
                dr.ReadingStatusLookupId,
                dr.ReadingStatusLookup != null ? dr.ReadingStatusLookup.Name : null,
                dr.RejectionReason,
                dr.Notes,
                dr.CreatedAt,
                dr.UpdatedAt,
                dr.ControllerId,
                dr.Controller != null ? dr.Controller.Name : null
            ))
            .FirstOrDefaultAsync();
    }
}
