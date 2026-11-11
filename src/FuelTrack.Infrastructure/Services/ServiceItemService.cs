using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class ServiceItemService : IServiceItemService
{
    private readonly FuelTrackDbContext _db;

    public ServiceItemService(FuelTrackDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ServiceItemDto>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.ServiceItems.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .Select(x => new ServiceItemDto(
                x.Id,
                x.Code,
                x.NameAr,
                x.NameEn,
                x.SortOrder,
                x.SuggestedIntervalHours,
                x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<Result<ServiceItemDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.ServiceItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (item is null)
        {
            return Result<ServiceItemDto>.NotFound($"Service item with ID {id} not found.");
        }

        return Result<ServiceItemDto>.Success(new ServiceItemDto(
            item.Id,
            item.Code,
            item.NameAr,
            item.NameEn,
            item.SortOrder,
            item.SuggestedIntervalHours,
            item.IsActive));
    }

    public async Task<Result<ServiceItemDto>> CreateAsync(CreateServiceItemDto dto, int userId, CancellationToken ct = default)
    {
        var code = dto.Code.Trim();
        var exists = await _db.ServiceItems
            .AnyAsync(x => x.Code.ToLower() == code.ToLower(), ct);

        if (exists)
        {
            return Result<ServiceItemDto>.Conflict($"Service item with code '{code}' already exists.");
        }

        var item = new ServiceItem
        {
            Code = code,
            NameAr = dto.NameAr.Trim(),
            NameEn = dto.NameEn.Trim(),
            SortOrder = dto.SortOrder,
            SuggestedIntervalHours = dto.SuggestedIntervalHours,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _db.ServiceItems.Add(item);
        await _db.SaveChangesAsync(ct);

        return Result<ServiceItemDto>.Success(new ServiceItemDto(
            item.Id,
            item.Code,
            item.NameAr,
            item.NameEn,
            item.SortOrder,
            item.SuggestedIntervalHours,
            item.IsActive), 201);
    }

    public async Task<Result<ServiceItemDto>> UpdateAsync(int id, UpdateServiceItemDto dto, int userId, CancellationToken ct = default)
    {
        var item = await _db.ServiceItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null)
        {
            return Result<ServiceItemDto>.NotFound($"Service item with ID {id} not found.");
        }

        item.NameAr = dto.NameAr.Trim();
        item.NameEn = dto.NameEn.Trim();
        item.SortOrder = dto.SortOrder;
        item.SuggestedIntervalHours = dto.SuggestedIntervalHours;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);

        return Result<ServiceItemDto>.Success(new ServiceItemDto(
            item.Id,
            item.Code,
            item.NameAr,
            item.NameEn,
            item.SortOrder,
            item.SuggestedIntervalHours,
            item.IsActive));
    }

    public async Task<Result<bool>> DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var item = await _db.ServiceItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null)
        {
            return Result<bool>.NotFound($"Service item with ID {id} not found.");
        }

        var hasDefinitions = await _db.ServiceIntervalDefinitions.AnyAsync(x => x.ServiceItemId == id, ct);
        if (hasDefinitions)
        {
            return Result<bool>.Conflict("Cannot delete service item because it is referenced by service interval definitions.");
        }

        var hasReplacements = await _db.ServiceReplacementRecords.AnyAsync(x => x.ServiceItemId == id, ct);
        if (hasReplacements)
        {
            return Result<bool>.Conflict("Cannot delete service item because it is referenced by service replacement records.");
        }

        item.DeletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
