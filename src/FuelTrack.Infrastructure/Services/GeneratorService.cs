using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Generators;
using FuelTrack.Application.DTOs.Generators.My;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class GeneratorService : IGeneratorService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;
    private readonly ICommercialStationModeService _commercialMode;

    public GeneratorService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, ICurrentUserService currentUser, ICommercialStationModeService? commercialMode = null)
    {
        _db = db;
        _scopeFilter = scopeFilter;
        _currentUser = currentUser;
        _commercialMode = commercialMode ?? new CommercialStationModeService(db);
    }

    public async Task<Result<GeneratorListDto>> CreateAsync(CreateGeneratorDto dto, int userId, CancellationToken ct = default)
    {
        var commercialScope = await GetCommercialScopeAsync(ct);
        var isCommercial = await _commercialMode.IsCommercialModeAsync(ct);
        if (isCommercial && commercialScope is null)
            return Result<GeneratorListDto>.Failure("Commercial mode has no active technical station scope.");

        if (isCommercial && !dto.DailyTankId.HasValue)
            return Result<GeneratorListDto>.Failure("A daily tank is required when creating a commercial generator.");

        var subStationId = commercialScope?.SubStationId ?? dto.SubStationId;
        var subStationExists = await _db.SubStations.AnyAsync(s => s.Id == subStationId, ct);
        if (!subStationExists)
            return Result<GeneratorListDto>.NotFound($"SubStation {subStationId} not found.");

        var duplicate = await _db.Generators.AnyAsync(g => g.SubStationId == subStationId && g.SerialNumber == dto.SerialNumber, ct);
        if (duplicate)
            return Result<GeneratorListDto>.Failure("Generator with this serial number already exists in this substation.");

        var generator = new Generator
        {
            SubStationId = subStationId,
            Name = dto.Name,
            SerialNumber = dto.SerialNumber,
            Model = dto.Model,
            Manufacturer = dto.Manufacturer,
            NominalPower = dto.NominalPower ?? 0,
            NominalPowerUnitLookupId = dto.NominalPowerUnitLookupId,
            ActualPowerKW = dto.ActualPowerKW,
            FuelConsumptionRatePerHour = dto.FuelConsumptionRatePerHour ?? 0m,
            OwnerName = dto.OwnerName,
            GeneratorTypeLookupId = dto.GeneratorTypeLookupId,
            VoltageLookupId = dto.VoltageLookupId,
            FrequencyLookupId = dto.FrequencyLookupId,
            PowerFactor = dto.PowerFactor,
            OwnershipTypeLookupId = dto.OwnershipTypeLookupId,
            StatusLookupId = dto.StatusLookupId,
            InstallationDate = dto.InstallationDate,
            Notes = dto.Notes,
            IsActive = dto.IsActive,
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

        // Validate the daily tank link before writing (commercial mode)
        FuelTank? linkedTank = null;
        if (isCommercial && dto.DailyTankId.HasValue)
        {
            var tankId = dto.DailyTankId.Value;
            linkedTank = await _db.FuelTanks.FindAsync(new object[] { tankId }, ct);
            if (linkedTank is null || linkedTank.DeletedAt is not null)
                return Result<GeneratorListDto>.NotFound($"Daily tank {tankId} not found.");
            if (linkedTank.TankRole != TankRole.Daily)
                return Result<GeneratorListDto>.Failure($"Tank {tankId} is not a daily tank.");
            if (linkedTank.SubStationId != subStationId)
                return Result<GeneratorListDto>.Failure("Daily tank does not belong to this station.");
            if (linkedTank.GeneratorId.HasValue)
                return Result<GeneratorListDto>.Failure("This tank is already linked to another generator.");
        }

        await DbTransactionRunner.RunAsync(_db, async transactionCt =>
        {
            _db.Generators.Add(generator);
            await _db.SaveChangesAsync(transactionCt);

            if (linkedTank is not null)
            {
                linkedTank.GeneratorId = generator.Id;
                await _db.SaveChangesAsync(transactionCt);
            }
        }, ct);

        var saved = await _db.Generators
            .Include(g => g.SubStation)
            .Include(g => g.GeneratorTypeLookup)
            .Include(g => g.VoltageLookup)
            .Include(g => g.FrequencyLookup)
            .Include(g => g.OwnershipTypeLookup)
            .Include(g => g.StatusLookup)
            .Include(g => g.Controller)
            .Include(g => g.FuelTanks)
            .FirstAsync(g => g.Id == generator.Id, ct);
        return Result<GeneratorListDto>.Success(MapToList(saved));
    }


    public async Task<Result<GeneratorDetailDto>> UpdateAsync(int id, UpdateGeneratorDto dto, int userId, CancellationToken ct = default)
    {
        var generator = await _db.Generators.FindAsync(new object[] { id }, ct);
        if (generator is null)
            return Result<GeneratorDetailDto>.NotFound($"Generator {id} not found.");

        var commercialScope = await GetCommercialScopeAsync(ct);
        if (await _commercialMode.IsCommercialModeAsync(ct) && commercialScope is null)
            return Result<GeneratorDetailDto>.Failure("Commercial mode has no active technical station scope.");
        if (commercialScope is not null && generator.SubStationId != commercialScope.SubStationId)
            return Result<GeneratorDetailDto>.NotFound($"Generator {id} not found.");

        if (commercialScope is null && dto.SubStationId.HasValue) generator.SubStationId = dto.SubStationId.Value;
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
        if (dto.IsActive.HasValue) generator.IsActive = dto.IsActive.Value;

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

        // Handle daily tank re-link in commercial mode
        if (dto.DailyTankId.HasValue)
        {
            var newTankId = dto.DailyTankId.Value;
            var newTank = await _db.FuelTanks.FindAsync(new object[] { newTankId }, ct);
            if (newTank is null || newTank.DeletedAt is not null)
                return Result<GeneratorDetailDto>.NotFound($"Daily tank {newTankId} not found.");
            if (newTank.TankRole != TankRole.Daily)
                return Result<GeneratorDetailDto>.Failure($"Tank {newTankId} is not a daily tank.");
            if (newTank.SubStationId != generator.SubStationId)
                return Result<GeneratorDetailDto>.Failure("Daily tank does not belong to this station.");
            if (newTank.GeneratorId.HasValue && newTank.GeneratorId.Value != generator.Id)
                return Result<GeneratorDetailDto>.Failure("This tank is already linked to another generator.");

            await DbTransactionRunner.RunAsync(_db, async transactionCt =>
            {
                var oldTank = await _db.FuelTanks
                    .FirstOrDefaultAsync(t => t.GeneratorId == generator.Id && t.DeletedAt == null, transactionCt);
                if (oldTank is not null && oldTank.Id != newTankId)
                    oldTank.GeneratorId = null;
                newTank.GeneratorId = generator.Id;
                await _db.SaveChangesAsync(transactionCt);
            }, ct);
        }
        else
        {
            await _db.SaveChangesAsync(ct);
        }

        var detail = (await MapToDetailAsync(generator.Id, ct))!;
        return Result<GeneratorDetailDto>.Success(detail);
    }

    public async Task<Result> DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var generator = await _db.Generators.FindAsync(new object[] { id }, ct);
        if (generator is null)
            return Result.NotFound($"Generator {id} not found.");
        var commercialScope = await GetCommercialScopeAsync(ct);
        if (await _commercialMode.IsCommercialModeAsync(ct) && commercialScope is null)
            return Result.Failure("Commercial mode has no active technical station scope.");
        if (commercialScope is not null && generator.SubStationId != commercialScope.SubStationId)
            return Result.NotFound($"Generator {id} not found.");

        generator.IsActive = false;
        generator.DeletedAt = DateTime.UtcNow;
        generator.UpdatedAt = DateTime.UtcNow;
        generator.UpdatedBy = userId;

        // Release the linked daily tank when generator is deleted
        var releasedTank = await _db.FuelTanks
            .FirstOrDefaultAsync(t => t.GeneratorId == generator.Id && t.DeletedAt == null, ct);
        if (releasedTank is not null)
            releasedTank.GeneratorId = null;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<GeneratorDetailDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
    {
        var commercialScope = await GetCommercialScopeAsync(ct);
        if (await _commercialMode.IsCommercialModeAsync(ct))
        {
            if (commercialScope is null) return null;
            return await MapToDetailAsync(id, ct, commercialScope.SubStationId);
        }
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
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
                    .Include(g => g.Controller)
                     .Where(g => g.Id == id && g.DeletedAt == null && accessibleIds.Contains(g.SubStationId))
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
                        g.IsActive,
                        g.Controller != null ? g.Controller.Name : null,
                        g.Controller != null ? g.Controller.Code : null,
                         g.CreatedAt,
                         g.UpdatedAt,
                         (int?)_db.FuelTanks.Where(t => t.GeneratorId == g.Id && t.DeletedAt == null && t.TankRole == TankRole.Daily).Select(t => t.Id).FirstOrDefault(),
                         _db.FuelTanks.Where(t => t.GeneratorId == g.Id && t.DeletedAt == null && t.TankRole == TankRole.Daily).Select(t => t.Name).FirstOrDefault()))
                    .FirstOrDefaultAsync(ct);
            }
        }

        return await MapToDetailAsync(id, ct);
    }

    public async Task<List<GeneratorListDto>> GetAllAsync(int userId, CancellationToken ct = default)
    {
        var commercialScope = await GetCommercialScopeAsync(ct);
        if (await _commercialMode.IsCommercialModeAsync(ct))
        {
            if (commercialScope is null) return [];
            return await _db.Generators
                .AsNoTracking()
                .Include(g => g.SubStation)
                .Include(g => g.GeneratorTypeLookup)
                .Include(g => g.VoltageLookup)
                .Include(g => g.FrequencyLookup)
                .Include(g => g.OwnershipTypeLookup)
                .Include(g => g.StatusLookup)
                .Include(g => g.Controller)
                .Include(g => g.FuelTanks)
                .Where(g => g.DeletedAt == null && g.SubStationId == commercialScope.SubStationId)
                .OrderBy(g => g.Name)
                .Select(g => MapToList(g))
                .ToListAsync(ct);
        }
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
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
                    .Include(g => g.Controller)
                .Include(g => g.FuelTanks)
                    .Where(g => g.DeletedAt == null && accessibleIds.Contains(g.SubStationId))
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
            .Include(g => g.Controller)
                .Include(g => g.FuelTanks)
            .Where(g => g.DeletedAt == null)
            .OrderBy(g => g.SubStation.Name)
            .ThenBy(g => g.Name)
            .Select(g => MapToList(g))
            .ToListAsync(ct);
    }

    public async Task<List<MyGeneratorListDto>> GetMyGeneratorsAsync(int userId, CancellationToken ct = default)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
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
            .Where(g => g.DeletedAt == null)
            .AsQueryable();

        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
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
            var approvedLookupId = await _db.Lookups
                .Where(l => l.Category != null && l.Category.Name == "ReadingStatus" && l.Code == "Approved")
                .Select(l => l.Id)
                .FirstOrDefaultAsync(ct);

            var lastReading = await _db.DailyReadings
                .Where(dr => dr.GeneratorId == g.Id && dr.ReadingStatusLookupId == approvedLookupId)
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

        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
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

        var approvedLookupId = await _db.Lookups
            .Where(l => l.Category != null && l.Category.Name == "ReadingStatus" && l.Code == "Approved")
            .Select(l => l.Id)
            .FirstOrDefaultAsync(ct);

        var lastReading = await _db.DailyReadings
            .Where(dr => dr.GeneratorId == g.Id && dr.ReadingStatusLookupId == approvedLookupId)
            .OrderByDescending(dr => dr.ReadingDate)
            .FirstOrDefaultAsync(ct);

        var lastPower = lastReading?.CurrentEnergyKWh ?? g.Controller?.EnergyReading;
        var lastHours = lastReading?.CurrentRunningHours ?? g.Controller?.OperatingHours;

        var assignedOils = await _db.Oils
            .Include(o => o.OilLookup)
            .Where(o => o.SubStationId == g.SubStationId)
            .Select(o => new AssignedOilTypeDto(o.OilLookupId, o.OilLookup!.Name, o.CurrentStock, o.MinStock))
            .ToListAsync(ct);

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

    public async Task<List<ActiveGeneratorDto>> GetActiveWithControllersAsync(int userId, CancellationToken ct = default)
    {
        var approvedLookupId = await _db.Lookups
            .Where(l => l.Category != null && l.Category.Name == "ReadingStatus" && l.Code == "Approved")
            .Select(l => l.Id)
            .FirstOrDefaultAsync(ct);

        var query = _db.Generators
            .AsNoTracking()
            .Include(g => g.SubStation)
                .ThenInclude(ss => ss.Station)
                    .ThenInclude(s => s.Branch)
            .Include(g => g.Controller)
            .Include(g => g.GeneratorTypeLookup)
            .Include(g => g.OwnershipTypeLookup)
            .Include(g => g.VoltageLookup)
            .Include(g => g.FrequencyLookup)
            .Include(g => g.StatusLookup)
            .Where(g => g.IsActive && g.Controller != null);

        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleIds.Count == 0)
                    return new List<ActiveGeneratorDto>();
                query = query.Where(g => accessibleIds.Contains(g.SubStationId));
            }
        }

        return await query
            .OrderBy(g => g.SubStation.Name)
            .ThenBy(g => g.Name)
            .Select(g => new ActiveGeneratorDto(
                g.Id,
                g.SubStation.Station.BranchId,
                g.SubStation.Station.Branch.Name,
                g.SubStation.StationId,
                g.SubStation.Station.Name,
                g.SubStationId,
                g.SubStation.Name,
                g.Name,
                g.SerialNumber,
                g.Model,
                g.Manufacturer,
                g.NominalPower,
                g.ActualPowerKW,
                g.GeneratorTypeLookup != null ? g.GeneratorTypeLookup.Name : null,
                g.OwnershipTypeLookup != null ? g.OwnershipTypeLookup.Name : null,
                g.VoltageLookup != null ? g.VoltageLookup.Name : null,
                g.FrequencyLookup != null ? g.FrequencyLookup.Name : null,
                g.StatusLookup != null ? g.StatusLookup.Name : null,
                g.IsActive,
                g.Controller != null ? g.Controller.Name : null,
                g.Controller != null ? g.Controller.Code : null,
                g.Controller != null ? g.Controller.ControllerType : null,
                g.DailyReadings
                    .Where(dr => dr.ReadingStatusLookupId == approvedLookupId)
                    .OrderByDescending(dr => dr.ReadingDate)
                    .Select(dr => (double?)dr.CurrentEnergyKWh)
                    .FirstOrDefault() ?? (g.Controller != null ? g.Controller.EnergyReading : 0),
                g.DailyReadings
                    .Where(dr => dr.ReadingStatusLookupId == approvedLookupId)
                    .OrderByDescending(dr => dr.ReadingDate)
                    .Select(dr => (double?)dr.CurrentRunningHours)
                    .FirstOrDefault() ?? (g.Controller != null ? g.Controller.OperatingHours : 0),
                g.SubStation.Name))
            .ToListAsync(ct);
    }

    public async Task<LastReadingDto?> GetLastReadingAsync(int generatorId, CancellationToken ct = default)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
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

        var approvedLookupId = await _db.Lookups
            .Where(l => l.Category != null && l.Category.Name == "ReadingStatus" && l.Code == "Approved")
            .Select(l => l.Id)
            .FirstOrDefaultAsync(ct);

        var reading = await _db.DailyReadings
            .Where(dr => dr.GeneratorId == generatorId && dr.ReadingStatusLookupId == approvedLookupId)
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

    public async Task<List<GeneratorOperationalCardDto>> GetOperationalCardsAsync(int userId, CancellationToken ct = default)
    {
        var query = _db.Generators
            .AsNoTracking()
            .Include(g => g.StatusLookup)
            .Where(g => g.IsActive);

        if (!_currentUser.HasAnyAdminRole)
        {
            var accessible = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId);
            if (accessible.Count == 0) return [];
            query = query.Where(g => accessible.Contains(g.SubStationId));
        }

        var generators = await query
            .OrderBy(g => g.Name)
            .Select(g => new
            {
                g.Id,
                g.Name,
                Status = g.StatusLookup != null ? g.StatusLookup.Name : null,
                ActualPowerKW = (decimal)g.ActualPowerKW,
                g.Manufacturer,
                g.Model,
            })
            .ToListAsync(ct);

        var genIds = generators.Select(g => g.Id).ToList();

        var latestOpReadings = await _db.OperationalGeneratorReadings.AsNoTracking()
            .Where(r => genIds.Contains(r.GeneratorId) && r.Status != OperationalReadingStatus.Voided && r.Status != OperationalReadingStatus.Rejected)
            .GroupBy(r => r.GeneratorId)
            .Select(g => g.OrderByDescending(r => r.CapturedAtUtc).ThenByDescending(r => r.Id).First())
            .ToListAsync(ct);
        var opMap = latestOpReadings.ToDictionary(r => r.GeneratorId);

        var meterReadings = await _db.MeterReadings.AsNoTracking()
            .Where(r => genIds.Contains(r.SubjectId)
                        && (r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours || r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced)
                        && ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .GroupBy(r => new { r.SubjectId, r.SubjectType })
            .Select(g => g.OrderByDescending(r => r.CapturedAtUtc).ThenByDescending(r => r.Id).First())
            .ToListAsync(ct);
        var hoursMap = meterReadings.Where(r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours).ToDictionary(r => r.SubjectId, r => r.RawValue * r.MultiplierSnapshot);
        var energyMap = meterReadings.Where(r => r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced).ToDictionary(r => r.SubjectId, r => r.RawValue * r.MultiplierSnapshot);

        var dailyTanks = await _db.FuelTanks.AsNoTracking()
            .Where(t => t.GeneratorId.HasValue && genIds.Contains(t.GeneratorId.Value))
            .Select(t => new { t.Id, GeneratorId = t.GeneratorId!.Value, t.Name, t.ActualStockLiters })
            .ToListAsync(ct);
        var tankMap = dailyTanks.GroupBy(t => t.GeneratorId).ToDictionary(g => g.Key, g => g.First());
        var tankIds = dailyTanks.Select(t => t.Id).ToList();

        var latestTankLevels = await _db.TankLevelReadings.AsNoTracking()
            .Where(r => tankIds.Contains(r.FuelTankId) && ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status))
            .GroupBy(r => r.FuelTankId)
            .Select(g => g.OrderByDescending(r => r.CapturedAtUtc).ThenByDescending(r => r.Id).First())
            .ToListAsync(ct);
        var tankLevelMap = latestTankLevels.ToDictionary(r => r.FuelTankId);

        var result = new List<GeneratorOperationalCardDto>(generators.Count);
        foreach (var g in generators)
        {
            opMap.TryGetValue(g.Id, out var op);
            hoursMap.TryGetValue(g.Id, out var hours);
            energyMap.TryGetValue(g.Id, out var energy);
            tankMap.TryGetValue(g.Id, out var tank);

            decimal? tankLevel = null;
            decimal? tankStock = null;
            if (tank != null)
            {
                if (tankLevelMap.TryGetValue(tank.Id, out var tl))
                {
                    tankLevel = tl.LiquidLevelCm;
                    tankStock = tl.ComputedLitres;
                }
                else if (tank.ActualStockLiters.HasValue)
                {
                    tankStock = (decimal)tank.ActualStockLiters.Value;
                }
            }

            result.Add(new GeneratorOperationalCardDto(
                g.Id,
                g.Name,
                g.Status,
                g.ActualPowerKW,
                g.Manufacturer,
                g.Model,
                op?.CapturedAtUtc,
                op?.IsRunning,
                op?.ActiveLoadKw,
                op?.PeakLoadKw,
                op?.LineVoltageVolts,
                op?.CurrentAmps,
                op?.FrequencyHz,
                op?.WaterTemperatureCelsius,
                op?.OilTemperatureCelsius,
                op?.OilPressure,
                op?.AirPressureBar,
                hours > 0 ? hours : null,
                energy > 0 ? energy : null,
                tank?.Name,
                tankLevel,
                tankStock));
        }

        return result;
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

    private async Task<GeneratorDetailDto?> MapToDetailAsync(int id, CancellationToken ct, int? requiredSubStationId = null)
    {
        return await _db.Generators
            .AsNoTracking()
            .Include(g => g.SubStation)
            .Include(g => g.NominalPowerUnitLookup)
            .Include(g => g.GeneratorTypeLookup)
            .Include(g => g.VoltageLookup)
            .Include(g => g.FrequencyLookup)
            .Include(g => g.StatusLookup)
            .Include(g => g.Controller)
            .Where(g => g.Id == id && g.DeletedAt == null && (!requiredSubStationId.HasValue || g.SubStationId == requiredSubStationId.Value))
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
                g.IsActive,
                g.Controller != null ? g.Controller.Name : null,
                g.Controller != null ? g.Controller.Code : null,
                g.CreatedAt,
                g.UpdatedAt,
                (int?)_db.FuelTanks.Where(t => t.GeneratorId == g.Id && t.DeletedAt == null && t.TankRole == TankRole.Daily).Select(t => t.Id).FirstOrDefault(),
                _db.FuelTanks.Where(t => t.GeneratorId == g.Id && t.DeletedAt == null && t.TankRole == TankRole.Daily).Select(t => t.Name).FirstOrDefault()))
            .FirstOrDefaultAsync(ct);
    }

    private async Task<CommercialStationScope?> GetCommercialScopeAsync(CancellationToken ct) =>
        await _commercialMode.IsCommercialModeAsync(ct)
            ? await _commercialMode.GetScopeAsync(ct)
            : null;

    private static GeneratorListDto MapToList(Generator g)
    {
        var dailyTank = g.FuelTanks.FirstOrDefault(t => t.TankRole == TankRole.Daily && t.DeletedAt == null);
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
            g.StatusLookup?.Name,
            g.IsActive,
            g.Controller?.Name,
            g.Controller?.Code,
            dailyTank?.Id,
            dailyTank?.Name);
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
}
