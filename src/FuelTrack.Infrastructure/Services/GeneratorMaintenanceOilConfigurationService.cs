using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class GeneratorMaintenanceOilConfigurationService : IGeneratorMaintenanceOilConfigurationService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;

    public GeneratorMaintenanceOilConfigurationService(
        FuelTrackDbContext db,
        IUserScopeFilterService scopeFilter,
        ICurrentUserService currentUser)
    {
        _db = db;
        _scopeFilter = scopeFilter;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<GeneratorMaintenanceOilConfigurationDto>> GetAllAsync(int? generatorId = null, CancellationToken ct = default)
    {
        var query = _db.GeneratorMaintenanceOilConfigurations.AsNoTracking().AsQueryable();
        if (generatorId.HasValue)
            query = query.Where(x => x.GeneratorId == generatorId.Value);

        if (!_currentUser.HasAnyAdminRole)
        {
            if (!_currentUser.UserId.HasValue) return [];
            var accessibleSubStations = await _scopeFilter.GetAccessibleSubStationIdsAsync(_currentUser.UserId.Value);
            query = query.Where(x => accessibleSubStations.Contains(x.Generator.SubStationId));
        }

        return await query
            .OrderBy(x => x.Generator.Name)
            .ThenBy(x => x.ServiceItem.SortOrder)
            .ThenBy(x => x.ServiceItem.Code)
            .Select(x => new GeneratorMaintenanceOilConfigurationDto(
                x.Id,
                x.GeneratorId,
                x.Generator.Name,
                x.ServiceItemId,
                x.ServiceItem.Code,
                x.ServiceItem.NameAr,
                x.ServiceItem.NameEn,
                x.OilId,
                x.Oil.OilLookupId,
                x.Oil.OilLookup.Name,
                x.QuantityLiters))
            .ToListAsync(ct);
    }

    public async Task<Result<GeneratorMaintenanceOilConfigurationDto>> SaveAsync(SaveGeneratorMaintenanceOilConfigurationDto dto, int userId, CancellationToken ct = default)
    {
        if (dto.QuantityLiters <= 0)
            return Result<GeneratorMaintenanceOilConfigurationDto>.Failure("Oil quantity must be greater than zero.", 400);

        var generator = await _db.Generators.FirstOrDefaultAsync(x => x.Id == dto.GeneratorId, ct);
        if (generator is null || !await HasAccessAsync(generator.SubStationId, userId))
            return Result<GeneratorMaintenanceOilConfigurationDto>.NotFound("Generator not found.");

        var serviceItem = await _db.ServiceItems.FirstOrDefaultAsync(x => x.Id == dto.ServiceItemId && x.IsActive, ct);
        if (serviceItem is null)
            return Result<GeneratorMaintenanceOilConfigurationDto>.NotFound("Active service item not found.");

        var oil = await _db.Oils
            .Include(x => x.OilLookup)
            .FirstOrDefaultAsync(x => x.Id == dto.OilId && x.SubStationId == generator.SubStationId, ct);
        if (oil is null)
            return Result<GeneratorMaintenanceOilConfigurationDto>.NotFound("Oil stock item not found for this generator.");

        var existing = await _db.GeneratorMaintenanceOilConfigurations
            .FirstOrDefaultAsync(x => x.GeneratorId == dto.GeneratorId && x.ServiceItemId == dto.ServiceItemId, ct);

        var now = DateTime.UtcNow;
        var isNew = existing is null;
        if (isNew)
        {
            existing = new GeneratorMaintenanceOilConfiguration
            {
                GeneratorId = generator.Id,
                ServiceItemId = serviceItem.Id,
                OilId = oil.Id,
                QuantityLiters = decimal.Round(dto.QuantityLiters, 3, MidpointRounding.AwayFromZero),
                CreatedAt = now,
                CreatedBy = userId,
                UpdatedAt = now,
                UpdatedBy = userId
            };
            _db.GeneratorMaintenanceOilConfigurations.Add(existing);
        }
        else
        {
            existing.OilId = oil.Id;
            existing.QuantityLiters = decimal.Round(dto.QuantityLiters, 3, MidpointRounding.AwayFromZero);
            existing.UpdatedAt = now;
            existing.UpdatedBy = userId;
        }

        await _db.SaveChangesAsync(ct);
        return Result<GeneratorMaintenanceOilConfigurationDto>.Success(new(
            existing.Id, generator.Id, generator.Name, serviceItem.Id, serviceItem.Code,
            serviceItem.NameAr, serviceItem.NameEn, oil.Id, oil.OilLookupId, oil.OilLookup.Name,
            existing.QuantityLiters), isNew ? 201 : 200);
    }

    public async Task<Result<bool>> DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var configuration = await _db.GeneratorMaintenanceOilConfigurations
            .Include(x => x.Generator)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (configuration is null || !await HasAccessAsync(configuration.Generator.SubStationId, userId))
            return Result<bool>.NotFound("Maintenance oil configuration not found.");

        configuration.DeletedAt = DateTime.UtcNow;
        configuration.UpdatedAt = DateTime.UtcNow;
        configuration.UpdatedBy = userId;
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    private async Task<bool> HasAccessAsync(int subStationId, int userId) =>
        _currentUser.HasAnyAdminRole || await _scopeFilter.HasAccessToSubStationAsync(userId, subStationId);
}
