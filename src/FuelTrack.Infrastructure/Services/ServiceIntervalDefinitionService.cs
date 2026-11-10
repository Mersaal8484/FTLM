using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class ServiceIntervalDefinitionService : IServiceIntervalDefinitionService
{
    private readonly FuelTrackDbContext _db;

    public ServiceIntervalDefinitionService(FuelTrackDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ServiceIntervalDefinitionDto>> GetAllAsync(string? model = null, CancellationToken ct = default)
    {
        var query = _db.ServiceIntervalDefinitions
            .AsNoTracking()
            .Include(x => x.ServiceItem)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(model))
        {
            var key = GeneratorModelKey.Normalize(model);
            query = query.Where(x => x.GeneratorModelKey == key);
        }

        var list = await query
            .OrderBy(x => x.GeneratorModelKey)
            .ThenBy(x => x.ServiceItem.SortOrder)
            .ThenBy(x => x.ServiceItem.Code)
            .ToListAsync(ct);

        return list.Select(x => new ServiceIntervalDefinitionDto(
            x.Id,
            x.GeneratorModelKey,
            x.ServiceItemId,
            x.ServiceItem.Code,
            x.ServiceItem.NameAr,
            x.ServiceItem.NameEn,
            x.IntervalHours,
            x.Importance,
            x.WarningLeadHours,
            ServiceDueCalculator.CalculateLeadHours(x.IntervalHours, x.WarningLeadHours),
            x.Notes)).ToList();
    }

    public async Task<Result<ServiceIntervalDefinitionDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.ServiceIntervalDefinitions
            .AsNoTracking()
            .Include(x => x.ServiceItem)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (item is null)
        {
            return Result<ServiceIntervalDefinitionDto>.NotFound($"Service interval definition with ID {id} not found.");
        }

        return Result<ServiceIntervalDefinitionDto>.Success(new ServiceIntervalDefinitionDto(
            item.Id,
            item.GeneratorModelKey,
            item.ServiceItemId,
            item.ServiceItem.Code,
            item.ServiceItem.NameAr,
            item.ServiceItem.NameEn,
            item.IntervalHours,
            item.Importance,
            item.WarningLeadHours,
            ServiceDueCalculator.CalculateLeadHours(item.IntervalHours, item.WarningLeadHours),
            item.Notes));
    }

    public async Task<Result<ServiceIntervalDefinitionDto>> SaveAsync(SaveServiceIntervalDefinitionDto dto, int userId, CancellationToken ct = default)
    {
        var modelKey = GeneratorModelKey.Normalize(dto.GeneratorModelKey);
        if (string.IsNullOrWhiteSpace(modelKey))
        {
            return Result<ServiceIntervalDefinitionDto>.Failure("Generator model key cannot be empty.");
        }

        var serviceItem = await _db.ServiceItems.FirstOrDefaultAsync(x => x.Id == dto.ServiceItemId, ct);
        if (serviceItem is null)
        {
            return Result<ServiceIntervalDefinitionDto>.NotFound($"Service item with ID {dto.ServiceItemId} not found.");
        }

        var existing = await _db.ServiceIntervalDefinitions
            .Include(x => x.ServiceItem)
            .FirstOrDefaultAsync(x => x.GeneratorModelKey == modelKey && x.ServiceItemId == dto.ServiceItemId, ct);

        if (existing is not null)
        {
            existing.IntervalHours = dto.IntervalHours;
            existing.Importance = dto.Importance;
            existing.WarningLeadHours = dto.WarningLeadHours;
            existing.Notes = dto.Notes?.Trim();
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = userId;

            await _db.SaveChangesAsync(ct);

            return Result<ServiceIntervalDefinitionDto>.Success(new ServiceIntervalDefinitionDto(
                existing.Id,
                existing.GeneratorModelKey,
                existing.ServiceItemId,
                serviceItem.Code,
                serviceItem.NameAr,
                serviceItem.NameEn,
                existing.IntervalHours,
                existing.Importance,
                existing.WarningLeadHours,
                ServiceDueCalculator.CalculateLeadHours(existing.IntervalHours, existing.WarningLeadHours),
                existing.Notes));
        }

        var definition = new ServiceIntervalDefinition
        {
            GeneratorModelKey = modelKey,
            ServiceItemId = dto.ServiceItemId,
            IntervalHours = dto.IntervalHours,
            Importance = dto.Importance,
            WarningLeadHours = dto.WarningLeadHours,
            Notes = dto.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _db.ServiceIntervalDefinitions.Add(definition);
        await _db.SaveChangesAsync(ct);

        return Result<ServiceIntervalDefinitionDto>.Success(new ServiceIntervalDefinitionDto(
            definition.Id,
            definition.GeneratorModelKey,
            definition.ServiceItemId,
            serviceItem.Code,
            serviceItem.NameAr,
            serviceItem.NameEn,
            definition.IntervalHours,
            definition.Importance,
            definition.WarningLeadHours,
            ServiceDueCalculator.CalculateLeadHours(definition.IntervalHours, definition.WarningLeadHours),
            definition.Notes), 201);
    }

    public async Task<Result<bool>> DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var item = await _db.ServiceIntervalDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null)
        {
            return Result<bool>.NotFound($"Service interval definition with ID {id} not found.");
        }

        item.DeletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
