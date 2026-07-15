using System.Security.Claims;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Generators;
using FuelTrack.Application.DTOs.Generators.My;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class GeneratorService : IGeneratorService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GeneratorService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, IHttpContextAccessor httpContextAccessor)
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

    private async Task EnsureLookupsExistAsync(CreateGeneratorDto dto, CancellationToken ct)
    {
        var pairs = new (string category, string? name)[]
        {
            ("GeneratorType", dto.GeneratorTypeLookupName),
            ("Voltage", dto.VoltageLookupName),
            ("Frequency", dto.FrequencyLookupName),
            ("OwnershipType", dto.OwnershipTypeLookupName),
            ("GeneratorStatus", dto.StatusLookupName),
        };

        var toCreate = new List<Lookup>();
        foreach (var (categoryName, lookupName) in pairs)
        {
            if (string.IsNullOrWhiteSpace(lookupName) || string.IsNullOrWhiteSpace(categoryName))
                continue;

            var category = await _db.LookupCategories.FirstOrDefaultAsync(c => c.Name == categoryName, ct);
            if (category == null) continue;

            var existing = await _db.Lookups.FirstOrDefaultAsync(l => l.CategoryId == category.Id && l.Name == lookupName, ct);
            if (existing == null)
            {
                toCreate.Add(new Lookup
                {
                    CategoryId = category.Id,
                    Name = lookupName,
                    Code = lookupName.ToUpperInvariant(),
                    CreatedAt = DateTime.UtcNow,
                });
            }
        }

        if (toCreate.Count > 0)
        {
            _db.Lookups.AddRange(toCreate);
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task EnsureLookupsExistForUpdateAsync(UpdateGeneratorDto dto, CancellationToken ct)
    {
        var pairs = new (string category, string? name)[]
        {
            ("GeneratorType", dto.GeneratorTypeLookupName),
            ("Voltage", dto.VoltageLookupName),
            ("Frequency", dto.FrequencyLookupName),
            ("OwnershipType", dto.OwnershipTypeLookupName),
            ("GeneratorStatus", dto.StatusLookupName),
        };

        var toCreate = new List<Lookup>();
        foreach (var (categoryName, lookupName) in pairs)
        {
            if (string.IsNullOrWhiteSpace(lookupName) || string.IsNullOrWhiteSpace(categoryName))
                continue;

            var category = await _db.LookupCategories.FirstOrDefaultAsync(c => c.Name == categoryName, ct);
            if (category == null) continue;

            var existing = await _db.Lookups.FirstOrDefaultAsync(l => l.CategoryId == category.Id && l.Name == lookupName, ct);
            if (existing == null)
            {
                toCreate.Add(new Lookup
                {
                    CategoryId = category.Id,
                    Name = lookupName,
                    Code = lookupName.ToUpperInvariant(),
                    CreatedAt = DateTime.UtcNow,
                });
            }
        }

        if (toCreate.Count > 0)
        {
            _db.Lookups.AddRange(toCreate);
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<int?> ResolveLookupIdAsync(string? categoryName, string? lookupName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(lookupName) || string.IsNullOrWhiteSpace(categoryName))
            return null;

        var category = await _db.LookupCategories
            .FirstOrDefaultAsync(c => c.Name == categoryName, ct);
        if (category == null) return null;

        var existing = await _db.Lookups
            .FirstOrDefaultAsync(l => l.CategoryId == category.Id && l.Name == lookupName, ct);
        return existing?.Id;
    }

    private async Task<Generator> ResolveLookupNamesAsync(Generator generator, CancellationToken ct)
    {
        generator.GeneratorTypeLookupId ??= await ResolveLookupIdAsync("GeneratorType", generator.GeneratorTypeLookup?.Name, ct);
        generator.VoltageLookupId ??= await ResolveLookupIdAsync("Voltage", generator.VoltageLookup?.Name, ct);
        generator.FrequencyLookupId ??= await ResolveLookupIdAsync("Frequency", generator.FrequencyLookup?.Name, ct);
        generator.OwnershipTypeLookupId ??= await ResolveLookupIdAsync("OwnershipType", generator.OwnershipTypeLookup?.Name, ct);
        generator.StatusLookupId ??= await ResolveLookupIdAsync("GeneratorStatus", generator.StatusLookup?.Name, ct);
        return generator;
    }

    public async Task<Result<GeneratorListDto>> CreateAsync(CreateGeneratorDto dto, int userId, CancellationToken ct = default)
    {
        var subStationExists = await _db.SubStations.AnyAsync(s => s.Id == dto.SubStationId, ct);
        if (!subStationExists)
            return Result<GeneratorListDto>.NotFound($"SubStation {dto.SubStationId} not found.");

        var duplicate = await _db.Generators.AnyAsync(g => g.SubStationId == dto.SubStationId && g.SerialNumber == dto.SerialNumber, ct);
        if (duplicate)
            return Result<GeneratorListDto>.Failure("Generator with this serial number already exists in this substation.");

        var generator = new Generator
        {
            SubStationId = dto.SubStationId,
            Name = dto.Name,
            SerialNumber = dto.SerialNumber,
            Model = dto.Model,
            Manufacturer = dto.Manufacturer,
            NominalPower = dto.NominalPower,
            NominalPowerUnitLookupId = dto.NominalPowerUnitLookupId,
            ActualPowerKW = dto.ActualPowerKW,
            FuelConsumptionRatePerHour = dto.FuelConsumptionRatePerHour,
            OwnerName = dto.OwnerName,
            GeneratorTypeLookupId = dto.GeneratorTypeLookupId,
            VoltageLookupId = dto.VoltageLookupId,
            FrequencyLookupId = dto.FrequencyLookupId,
            PowerFactor = dto.PowerFactor,
            OwnershipTypeLookupId = dto.OwnershipTypeLookupId,
            StatusLookupId = dto.StatusLookupId,
            InstallationDate = dto.InstallationDate,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
        };

        await EnsureLookupsExistAsync(dto, ct);

        if (generator.GeneratorTypeLookupId == null && !string.IsNullOrWhiteSpace(dto.GeneratorTypeLookupName))
            generator.GeneratorTypeLookupId = await ResolveLookupIdAsync("GeneratorType", dto.GeneratorTypeLookupName, ct);
        if (generator.VoltageLookupId == null && !string.IsNullOrWhiteSpace(dto.VoltageLookupName))
            generator.VoltageLookupId = await ResolveLookupIdAsync("Voltage", dto.VoltageLookupName, ct);
        if (generator.FrequencyLookupId == null && !string.IsNullOrWhiteSpace(dto.FrequencyLookupName))
            generator.FrequencyLookupId = await ResolveLookupIdAsync("Frequency", dto.FrequencyLookupName, ct);
        if (generator.OwnershipTypeLookupId == null && !string.IsNullOrWhiteSpace(dto.OwnershipTypeLookupName))
            generator.OwnershipTypeLookupId = await ResolveLookupIdAsync("OwnershipType", dto.OwnershipTypeLookupName, ct);
        if (generator.StatusLookupId == null && !string.IsNullOrWhiteSpace(dto.StatusLookupName))
            generator.StatusLookupId = await ResolveLookupIdAsync("GeneratorStatus", dto.StatusLookupName, ct);

        _db.Generators.Add(generator);
        await _db.SaveChangesAsync(ct);

        var saved = await _db.Generators
            .Include(g => g.SubStation)
            .FirstAsync(g => g.Id == generator.Id, ct);
        return Result<GeneratorListDto>.Success(MapToList(saved));
    }

    public async Task<Result<GeneratorDetailDto>> UpdateAsync(int id, UpdateGeneratorDto dto, int userId, CancellationToken ct = default)
    {
        var generator = await _db.Generators.FindAsync(new object[] { id }, ct);
        if (generator is null)
            return Result<GeneratorDetailDto>.NotFound($"Generator {id} not found.");

        if (dto.SubStationId.HasValue) generator.SubStationId = dto.SubStationId.Value;
        if (dto.Name is not null) generator.Name = dto.Name;
        if (dto.SerialNumber is not null) generator.SerialNumber = dto.SerialNumber;
        if (dto.Model is not null) generator.Model = dto.Model;
        if (dto.Manufacturer is not null) generator.Manufacturer = dto.Manufacturer;
        if (dto.NominalPower.HasValue) generator.NominalPower = dto.NominalPower.Value;
        if (dto.NominalPowerUnitLookupId.HasValue) generator.NominalPowerUnitLookupId = dto.NominalPowerUnitLookupId.Value;
        if (dto.ActualPowerKW.HasValue) generator.ActualPowerKW = dto.ActualPowerKW.Value;
        if (dto.FuelConsumptionRatePerHour.HasValue) generator.FuelConsumptionRatePerHour = dto.FuelConsumptionRatePerHour.Value;
        if (dto.OwnerName is not null) generator.OwnerName = dto.OwnerName;
        if (dto.PowerFactor.HasValue) generator.PowerFactor = dto.PowerFactor.Value;
        if (dto.InstallationDate.HasValue) generator.InstallationDate = dto.InstallationDate.Value;
        if (dto.Notes is not null) generator.Notes = dto.Notes;

        await EnsureLookupsExistForUpdateAsync(dto, ct);

        if (dto.GeneratorTypeLookupId.HasValue)
            generator.GeneratorTypeLookupId = dto.GeneratorTypeLookupId.Value;
        else if (!string.IsNullOrWhiteSpace(dto.GeneratorTypeLookupName))
            generator.GeneratorTypeLookupId = await ResolveLookupIdAsync("GeneratorType", dto.GeneratorTypeLookupName, ct);

        if (dto.VoltageLookupId.HasValue)
            generator.VoltageLookupId = dto.VoltageLookupId.Value;
        else if (!string.IsNullOrWhiteSpace(dto.VoltageLookupName))
            generator.VoltageLookupId = await ResolveLookupIdAsync("Voltage", dto.VoltageLookupName, ct);

        if (dto.FrequencyLookupId.HasValue)
            generator.FrequencyLookupId = dto.FrequencyLookupId.Value;
        else if (!string.IsNullOrWhiteSpace(dto.FrequencyLookupName))
            generator.FrequencyLookupId = await ResolveLookupIdAsync("Frequency", dto.FrequencyLookupName, ct);

        if (dto.OwnershipTypeLookupId.HasValue)
            generator.OwnershipTypeLookupId = dto.OwnershipTypeLookupId.Value;
        else if (!string.IsNullOrWhiteSpace(dto.OwnershipTypeLookupName))
            generator.OwnershipTypeLookupId = await ResolveLookupIdAsync("OwnershipType", dto.OwnershipTypeLookupName, ct);

        if (dto.StatusLookupId.HasValue)
            generator.StatusLookupId = dto.StatusLookupId.Value;
        else if (!string.IsNullOrWhiteSpace(dto.StatusLookupName))
            generator.StatusLookupId = await ResolveLookupIdAsync("GeneratorStatus", dto.StatusLookupName, ct);

        generator.UpdatedAt = DateTime.UtcNow;
        generator.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);

        var detail = (await MapToDetailAsync(generator.Id, ct))!;
        return Result<GeneratorDetailDto>.Success(detail);
    }

    public async Task<Result> DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var generator = await _db.Generators.FindAsync(new object[] { id }, ct);
        if (generator is null)
            return Result.NotFound($"Generator {id} not found.");

        var hasReadings = await _db.DailyReadings.AnyAsync(dr => dr.GeneratorId == id, ct);
        if (hasReadings)
            return Result.Failure("Cannot delete a generator with associated readings.");

        _db.Generators.Remove(generator);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<GeneratorDetailDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return null;
                return await _db.Generators
                    .AsNoTracking()
                    .Include(g => g.SubStation)
                    .Include(g => g.NominalPowerUnitLookup)
                    .Include(g => g.GeneratorTypeLookup)
                    .Include(g => g.VoltageLookup)
                    .Include(g => g.FrequencyLookup)
                    .Include(g => g.StatusLookup)
                    .Where(g => g.Id == id && accessibleIds.Contains(g.SubStationId))
                    .Select(g => new GeneratorDetailDto(
                        g.Id,
                        g.SubStationId,
                        g.SubStation.Name,
                        g.Name,
                        g.SerialNumber,
                        g.Model,
                        g.Manufacturer,
                        g.NominalPower,
                        g.NominalPowerUnitLookupId,
                        g.NominalPowerUnitLookup != null ? g.NominalPowerUnitLookup.Name : null,
                        g.ActualPowerKW,
                        g.FuelConsumptionRatePerHour,
                        g.OwnerName,
                        g.GeneratorTypeLookupId,
                        g.GeneratorTypeLookup != null ? g.GeneratorTypeLookup.Name : null,
                        g.VoltageLookupId,
                        g.VoltageLookup != null ? g.VoltageLookup.Name : null,
                        g.FrequencyLookupId,
                        g.FrequencyLookup != null ? g.FrequencyLookup.Name : null,
                        g.PowerFactor,
                        g.OwnershipTypeLookupId,
                        g.OwnershipTypeLookup != null ? g.OwnershipTypeLookup.Name : null,
                        g.StatusLookupId,
                        g.StatusLookup != null ? g.StatusLookup.Name : null,
                        g.InstallationDate,
                        g.Notes,
                        g.CreatedAt,
                        g.UpdatedAt))
                    .FirstOrDefaultAsync(ct);
            }
        }

        return await MapToDetailAsync(id, ct);
    }

    public async Task<List<GeneratorListDto>> GetAllAsync(int userId, CancellationToken ct = default)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<GeneratorListDto>();
                return await _db.Generators
                    .AsNoTracking()
                    .Include(g => g.SubStation)
                    .Include(g => g.GeneratorTypeLookup)
                    .Include(g => g.VoltageLookup)
                    .Include(g => g.FrequencyLookup)
                    .Include(g => g.OwnershipTypeLookup)
                    .Include(g => g.StatusLookup)
                    .Where(g => accessibleIds.Contains(g.SubStationId))
                    .OrderBy(g => g.SubStation.Name)
                    .ThenBy(g => g.Name)
                    .Select(g => MapToList(g))
                    .ToListAsync(ct);
            }
        }

        return await _db.Generators
            .AsNoTracking()
            .Include(g => g.SubStation)
            .Include(g => g.GeneratorTypeLookup)
            .Include(g => g.VoltageLookup)
            .Include(g => g.FrequencyLookup)
            .Include(g => g.OwnershipTypeLookup)
            .Include(g => g.StatusLookup)
            .OrderBy(g => g.SubStation.Name)
            .ThenBy(g => g.Name)
            .Select(g => MapToList(g))
            .ToListAsync(ct);
    }

    public async Task<List<MyGeneratorListDto>> GetMyGeneratorsAsync(int userId, CancellationToken ct = default)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<MyGeneratorListDto>();
            }
        }

        var query = _db.Generators
            .Include(g => g.SubStation).ThenInclude(s => s!.Station)
            .Include(g => g.GeneratorTypeLookup)
            .Include(g => g.StatusLookup)
            .Include(g => g.Controller)
            .AsQueryable();

        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                query = query.Where(g => accessibleIds.Contains(g.SubStationId));
            }
        }

        var generators = await query
            .OrderBy(g => g.SubStation!.Name)
            .ThenBy(g => g.Name)
            .ToListAsync(ct);

        var result = new List<MyGeneratorListDto>();
        foreach (var g in generators)
        {
            var lastReading = await _db.DailyReadings
                .Where(dr => dr.GeneratorId == g.Id)
                .OrderByDescending(dr => dr.ReadingDate)
                .FirstOrDefaultAsync(ct);

            var lastPower = lastReading?.CurrentEnergyKWh ?? g.Controller?.EnergyReading;
            var lastHours = lastReading?.CurrentRunningHours ?? g.Controller?.OperatingHours;

            var assignedOils = await _db.Oils
                .Include(o => o.OilLookup)
                .Where(o => o.SubStationId == g.SubStationId)
                .Select(o => new AssignedOilTypeDto(o.OilLookupId, o.OilLookup!.Name, o.CurrentStock, o.MinStock))
                .ToListAsync(ct);

            var statusName = g.StatusLookup?.Name;
            var status = statusName switch
            {
                "شغال" => "Running",
                "واقف" => "Idle",
                "تحت الصيانة" => "Maintenance",
                _ => "Offline"
            };

            result.Add(new MyGeneratorListDto(
                g.Id,
                g.SubStationId,
                g.SubStation?.Name ?? "",
                g.SubStation?.StationId,
                g.SubStation?.Station?.Name,
                g.Name,
                g.SerialNumber,
                g.NominalPower,
                g.ActualPowerKW,
                g.FuelConsumptionRatePerHour,
                g.GeneratorTypeLookup?.Name,
                status,
                lastPower,
                lastHours,
                assignedOils,
                null,
                null,
                null));
        }
        return result;
    }

    public async Task<MyGeneratorDetailDto?> GetMyGeneratorByIdAsync(int id, int userId, CancellationToken ct = default)
    {
        Generator? g = null;

        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return null;
                g = await _db.Generators
                    .Include(g => g.SubStation).ThenInclude(s => s!.Station)
                    .Include(g => g.GeneratorTypeLookup)
                    .Include(g => g.StatusLookup)
                    .Where(g => g.Id == id && accessibleIds.Contains(g.SubStationId))
                    .FirstOrDefaultAsync(ct);
                if (g == null) return null;
            }
        }

        if (g == null)
        {
            g = await _db.Generators
                .Include(g => g.SubStation).ThenInclude(s => s!.Station)
                .Include(g => g.GeneratorTypeLookup)
                .Include(g => g.StatusLookup)
                .Include(g => g.Controller)
                .FirstOrDefaultAsync(g => g.Id == id, ct);
        }

        if (g == null) return null;

        var lastReading = await _db.DailyReadings
            .Where(dr => dr.GeneratorId == g.Id)
            .OrderByDescending(dr => dr.ReadingDate)
            .FirstOrDefaultAsync(ct);

        var lastPower = lastReading?.CurrentEnergyKWh ?? g.Controller?.EnergyReading;
        var lastHours = lastReading?.CurrentRunningHours ?? g.Controller?.OperatingHours;

        var assignedOils = await _db.Oils
            .Include(o => o.OilLookup)
            .Where(o => o.SubStationId == g.SubStationId)
            .Select(o => new AssignedOilTypeDto(o.OilLookupId, o.OilLookup!.Name, o.CurrentStock, o.MinStock))
            .ToListAsync(ct);

        // Derive the fuel type(s) the generator consumes from the sub-station's fuel tanks.
        var fuelLookups = await _db.FuelTanks
            .Include(ft => ft.FuelLookup)
            .Where(ft => ft.SubStationId == g.SubStationId && ft.FuelLookup != null)
            .Select(ft => new { ft.FuelLookupId, ft.FuelLookup!.Name })
            .Distinct()
            .ToListAsync(ct);

        int? fuelLookupId = fuelLookups.Count == 1 ? fuelLookups[0].FuelLookupId : null;
        string? fuelLookupName = fuelLookups.Count > 0
            ? string.Join(" / ", fuelLookups.Select(f => f.Name))
            : null;

        var statusName = g.StatusLookup?.Name;
        var status = statusName switch
        {
            "شغال" => "Running",
            "واقف" => "Idle",
            "تحت الصيانة" => "Maintenance",
            _ => "Offline"
        };

        return new MyGeneratorDetailDto(
            g.Id,
            g.SubStationId,
            g.SubStation?.Name ?? "",
            g.SubStation?.StationId,
            g.SubStation?.Station?.Name,
            g.Name,
            g.SerialNumber,
            g.NominalPower,
            g.ActualPowerKW,
            g.FuelConsumptionRatePerHour,
            g.GeneratorTypeLookup?.Name,
            status,
            lastPower,
            lastHours,
            assignedOils,
            fuelLookupId,
            fuelLookupName,
            null,
            null,
            null,
            null,
            g.OwnerName);
    }

    public async Task<LastReadingDto?> GetLastReadingAsync(int generatorId, CancellationToken ct = default)
    {
        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return null;
                var generator = await _db.Generators
                    .FirstOrDefaultAsync(g => g.Id == generatorId && accessibleIds.Contains(g.SubStationId), ct);
                if (generator == null)
                    return null;
            }
        }

        var reading = await _db.DailyReadings
            .Where(dr => dr.GeneratorId == generatorId)
            .OrderByDescending(dr => dr.ReadingDate)
            .FirstOrDefaultAsync(ct);

        if (reading != null)
        {
            return new LastReadingDto(reading.CurrentEnergyKWh, reading.CurrentRunningHours, reading.ReadingDate, "daily_reading");
        }

        var controller = await _db.Controllers
            .FirstOrDefaultAsync(c => c.GeneratorId == generatorId, ct);

        if (controller != null)
        {
            return new LastReadingDto(controller.EnergyReading, controller.OperatingHours, null, "controller");
        }

        return null;
    }

    private async Task<GeneratorDetailDto?> MapToDetailAsync(int id, CancellationToken ct)
    {
        return await _db.Generators
            .AsNoTracking()
            .Include(g => g.SubStation)
            .Include(g => g.NominalPowerUnitLookup)
            .Include(g => g.GeneratorTypeLookup)
            .Include(g => g.VoltageLookup)
            .Include(g => g.FrequencyLookup)
            .Include(g => g.OwnershipTypeLookup)
            .Include(g => g.StatusLookup)
            .Where(g => g.Id == id)
            .Select(g => new GeneratorDetailDto(
                g.Id,
                g.SubStationId,
                g.SubStation.Name,
                g.Name,
                g.SerialNumber,
                g.Model,
                g.Manufacturer,
                g.NominalPower,
                g.NominalPowerUnitLookupId,
                g.NominalPowerUnitLookup != null ? g.NominalPowerUnitLookup.Name : null,
                g.ActualPowerKW,
                g.FuelConsumptionRatePerHour,
                g.OwnerName,
                g.GeneratorTypeLookupId,
                g.GeneratorTypeLookup != null ? g.GeneratorTypeLookup.Name : null,
                g.VoltageLookupId,
                g.VoltageLookup != null ? g.VoltageLookup.Name : null,
                g.FrequencyLookupId,
                g.FrequencyLookup != null ? g.FrequencyLookup.Name : null,
                g.PowerFactor,
                g.OwnershipTypeLookupId,
                g.OwnershipTypeLookup != null ? g.OwnershipTypeLookup.Name : null,
                g.StatusLookupId,
                g.StatusLookup != null ? g.StatusLookup.Name : null,
                g.InstallationDate,
                g.Notes,
                g.CreatedAt,
                g.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    private static GeneratorListDto MapToList(Generator g)
    {
        return new GeneratorListDto(
            g.Id,
            g.SubStationId,
            g.SubStation.Name,
            g.Name,
            g.SerialNumber,
            g.Model,
            g.Manufacturer,
            g.NominalPower,
            g.ActualPowerKW,
            g.FuelConsumptionRatePerHour,
            g.OwnerName,
            g.GeneratorTypeLookupId,
            g.GeneratorTypeLookup?.Name,
            g.VoltageLookupId,
            g.VoltageLookup?.Name,
            g.FrequencyLookupId,
            g.FrequencyLookup?.Name,
            g.OwnershipTypeLookupId,
            g.OwnershipTypeLookup?.Name,
            g.StatusLookupId,
            g.StatusLookup?.Name);
    }
}
