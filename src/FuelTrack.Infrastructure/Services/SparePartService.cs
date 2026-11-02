using FuelTrack.Application;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class SparePartService : ISparePartService
{
    private readonly FuelTrackDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserScopeFilterService _scopeFilter;

    public SparePartService(FuelTrackDbContext db, ICurrentUserService currentUser, IUserScopeFilterService scopeFilter)
    {
        _db = db;
        _currentUser = currentUser;
        _scopeFilter = scopeFilter;
    }

    public async Task<Result<SparePartDetailDto>> CreateAsync(CreateSparePartDto dto, int userId, CancellationToken ct = default)
    {
        var duplicate = await _db.SpareParts.AnyAsync(sp => sp.PartNumber == dto.PartNumber && sp.DeletedAt == null, ct);
        if (duplicate)
            return Result<SparePartDetailDto>.Failure("Spare part with this part number already exists.");

        var category = await _db.Lookups.FirstOrDefaultAsync(l => l.Id == dto.PartCategoryLookupId, ct);
        if (category == null)
            return Result<SparePartDetailDto>.NotFound("Part category not found.");

        var unit = await _db.Lookups.FirstOrDefaultAsync(l => l.Id == dto.UnitLookupId, ct);
        if (unit == null)
            return Result<SparePartDetailDto>.NotFound("Unit not found.");

        if (dto.ReplacementPriorityLookupId.HasValue)
        {
            var priority = await _db.Lookups.FirstOrDefaultAsync(l => l.Id == dto.ReplacementPriorityLookupId.Value, ct);
            if (priority == null)
                return Result<SparePartDetailDto>.NotFound("Replacement priority not found.");
        }

        var sparePart = new SparePart
        {
            Name = dto.Name,
            NameEn = dto.NameEn,
            PartNumber = dto.PartNumber,
            ManufacturerPartNumber = dto.ManufacturerPartNumber,
            Barcode = dto.Barcode,
            PartCategoryLookupId = dto.PartCategoryLookupId,
            UnitLookupId = dto.UnitLookupId,
            Description = dto.Description,
            UnitPrice = dto.UnitPrice,
            MinStockQuantity = dto.MinStockQuantity,
            ReplacementIntervalHours = dto.ReplacementIntervalHours,
            ReplacementPriorityLookupId = dto.ReplacementPriorityLookupId,
            IsHoursTracked = dto.IsHoursTracked,
            Notes = dto.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _db.SpareParts.Add(sparePart);
        await _db.SaveChangesAsync(ct);

        var detail = await MapToDetailAsync(sparePart.Id, ct);
        return Result<SparePartDetailDto>.Success(detail!);
    }

    public async Task<Result<SparePartDetailDto>> UpdateAsync(int id, UpdateSparePartDto dto, int userId, CancellationToken ct = default)
    {
        var sparePart = await _db.SpareParts.FindAsync(new object[] { id }, ct);
        if (sparePart is null)
            return Result<SparePartDetailDto>.NotFound($"Spare part {id} not found.");

        if (dto.Name is not null) sparePart.Name = dto.Name;
        if (dto.NameEn is not null) sparePart.NameEn = dto.NameEn;
        if (dto.PartNumber is not null) sparePart.PartNumber = dto.PartNumber;
        if (dto.ManufacturerPartNumber is not null) sparePart.ManufacturerPartNumber = dto.ManufacturerPartNumber;
        if (dto.Barcode is not null) sparePart.Barcode = dto.Barcode;
        if (dto.PartCategoryLookupId.HasValue) sparePart.PartCategoryLookupId = dto.PartCategoryLookupId.Value;
        if (dto.UnitLookupId.HasValue) sparePart.UnitLookupId = dto.UnitLookupId.Value;
        if (dto.Description is not null) sparePart.Description = dto.Description;
        if (dto.UnitPrice.HasValue) sparePart.UnitPrice = dto.UnitPrice.Value;
        if (dto.MinStockQuantity.HasValue) sparePart.MinStockQuantity = dto.MinStockQuantity.Value;
        if (dto.ReplacementIntervalHours.HasValue) sparePart.ReplacementIntervalHours = dto.ReplacementIntervalHours.Value;
        if (dto.IsHoursTracked.HasValue) sparePart.IsHoursTracked = dto.IsHoursTracked.Value;
        if (dto.ReplacementPriorityLookupId.HasValue) sparePart.ReplacementPriorityLookupId = dto.ReplacementPriorityLookupId.Value;
        if (dto.Notes is not null) sparePart.Notes = dto.Notes;
        if (dto.IsActive.HasValue) sparePart.IsActive = dto.IsActive.Value;

        sparePart.UpdatedAt = DateTime.UtcNow;
        sparePart.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);

        var detail = await MapToDetailAsync(sparePart.Id, ct);
        return Result<SparePartDetailDto>.Success(detail!);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var sparePart = await _db.SpareParts.FindAsync(new object[] { id }, ct);
        if (sparePart is null)
            return Result.NotFound($"Spare part {id} not found.");

        sparePart.IsActive = false;
        sparePart.DeletedAt = DateTime.UtcNow;
        sparePart.UpdatedAt = DateTime.UtcNow;
        sparePart.UpdatedBy = 0;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<SparePartDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await MapToDetailAsync(id, ct);
    }

    public async Task<List<SparePartListDto>> GetAllAsync(int? stationId, CancellationToken ct = default)
    {
        var query = _db.SpareParts
            .AsNoTracking()
            .Include(sp => sp.PartCategoryLookup)
            .Include(sp => sp.UnitLookup)
            .Include(sp => sp.ReplacementPriorityLookup)
            .Where(sp => sp.DeletedAt == null)
            .AsQueryable();

        if (stationId.HasValue)
        {
            query = query.Where(sp => sp.Stocks.Any(ws => ws.StationId == stationId.Value && ws.DeletedAt == null));
        }

        return await query
            .OrderBy(sp => sp.PartNumber)
            .Select(sp => new SparePartListDto(
                sp.Id,
                sp.PartNumber,
                sp.Name,
                sp.PartCategoryLookup.Name,
                sp.UnitLookup.Name,
                sp.MinStockQuantity,
                sp.ReplacementIntervalHours,
                sp.IsHoursTracked,
                sp.UnitPrice,
                sp.Notes,
                sp.IsActive))
            .ToListAsync(ct);
    }

    public async Task<PagedResult<SparePartListDto>> GetPagedAsync(int page, int pageSize, string? search = null, int? categoryId = null, string? category = null, CancellationToken ct = default)
    {
        var query = _db.SpareParts
            .AsNoTracking()
            .Include(sp => sp.PartCategoryLookup)
            .Include(sp => sp.UnitLookup)
            .Include(sp => sp.ReplacementPriorityLookup)
            .Where(sp => sp.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(sp => sp.Name.Contains(search) || sp.PartNumber.Contains(search));

        if (categoryId.HasValue)
            query = query.Where(sp => sp.PartCategoryLookupId == categoryId.Value);
        else if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(sp => sp.PartCategoryLookup.Name == category);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(sp => sp.PartNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sp => new SparePartListDto(
                sp.Id,
                sp.PartNumber,
                sp.Name,
                sp.PartCategoryLookup.Name,
                sp.UnitLookup.Name,
                sp.MinStockQuantity,
                sp.ReplacementIntervalHours,
                sp.IsHoursTracked,
                sp.UnitPrice,
                sp.Notes,
                sp.IsActive))
            .ToListAsync(ct);

        return new PagedResult<SparePartListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<WarehouseStockDto>> GetStockByPartAsync(int sparePartId, CancellationToken ct = default)
    {
        return await _db.WarehouseStocks
            .AsNoTracking()
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.PartCategoryLookup)
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.UnitLookup)
            .Include(ws => ws.Station)
            .Where(ws => ws.SparePartId == sparePartId && ws.DeletedAt == null)
            .Select(ws => new WarehouseStockDto(
                ws.SparePartId,
                ws.SparePart.PartNumber,
                ws.SparePart.Name,
                ws.SparePart.PartCategoryLookup.Name,
                ws.SparePart.UnitLookup.Name,
                ws.StationId,
                ws.Station.Name,
                ws.CurrentQuantity,
                ws.ReservedQuantity,
                ws.AvailableQuantity,
                ws.SparePart.MinStockQuantity,
                ws.CurrentQuantity < ws.SparePart.MinStockQuantity))
            .ToListAsync(ct);
    }

    public async Task<string> ExportCsvAsync(string? search = null, int? categoryId = null, string? category = null, CancellationToken ct = default)
    {
        var query = _db.SpareParts
            .AsNoTracking()
            .Include(sp => sp.PartCategoryLookup)
            .Include(sp => sp.UnitLookup)
            .Where(sp => sp.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(sp => sp.Name.Contains(search) || sp.PartNumber.Contains(search));

        if (categoryId.HasValue)
            query = query.Where(sp => sp.PartCategoryLookupId == categoryId.Value);

        var items = await query
            .OrderBy(sp => sp.PartNumber)
            .Select(sp => new
            {
                sp.PartNumber,
                sp.Name,
                Category = sp.PartCategoryLookup.Name,
                Unit = sp.UnitLookup.Name,
                sp.MinStockQuantity,
                sp.UnitPrice,
                sp.IsActive
            })
            .ToListAsync(ct);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("PartNumber,Name,Category,Unit,MinStock,UnitPrice,IsActive");
        foreach (var item in items)
        {
            sb.AppendLine($"{item.PartNumber},{item.Name},{item.Category},{item.Unit},{item.MinStockQuantity},{item.UnitPrice},{item.IsActive}");
        }
        return sb.ToString();
    }

    private async Task<SparePartDetailDto?> MapToDetailAsync(int id, CancellationToken ct)
    {
        return await _db.SpareParts
            .AsNoTracking()
            .Include(sp => sp.PartCategoryLookup)
            .Include(sp => sp.UnitLookup)
            .Include(sp => sp.ReplacementPriorityLookup)
            .Where(sp => sp.Id == id && sp.DeletedAt == null)
            .Select(sp => new SparePartDetailDto(
                sp.Id,
                sp.PartNumber,
                sp.Name,
                sp.NameEn,
                sp.ManufacturerPartNumber,
                sp.Barcode,
                sp.PartCategoryLookupId,
                sp.PartCategoryLookup.Name,
                sp.UnitLookupId,
                sp.UnitLookup.Name,
                sp.Description,
                sp.UnitPrice,
                sp.MinStockQuantity,
                sp.ReplacementIntervalHours,
                sp.IsHoursTracked,
                sp.ReplacementPriorityLookupId,
                sp.ReplacementPriorityLookup != null ? sp.ReplacementPriorityLookup.Name : null,
                sp.Notes,
                sp.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
