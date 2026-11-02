using FuelTrack.Application;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class WarehouseSupplyService : IWarehouseSupplyService
{
    private readonly FuelTrackDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserScopeFilterService _scopeFilter;

    public WarehouseSupplyService(FuelTrackDbContext db, ICurrentUserService currentUser, IUserScopeFilterService scopeFilter)
    {
        _db = db;
        _currentUser = currentUser;
        _scopeFilter = scopeFilter;
    }

    public async Task<WarehouseSupplyDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleStationIds = await _scopeFilter.GetAccessibleStationIdsAsync(currentUserId.Value);
                var supply = await _db.WarehouseSupplies
                    .AsNoTracking()
                    .Include(ws => ws.Station)
                    .Include(ws => ws.StatusLookup)
                    .Include(ws => ws.Items)
                        .ThenInclude(i => i.SparePart)
                    .Where(ws => ws.Id == id && ws.DeletedAt == null && accessibleStationIds.Contains(ws.StationId))
                    .Select(ws => new WarehouseSupplyDetailDto(
                        ws.Id,
                        ws.SupplyNumber,
                        ws.StationId,
                        ws.Station.Name,
                        ws.SupplyDate,
                        ws.SupplierName,
                        ws.InvoiceNumber,
                        ws.InvoiceDate,
                        ws.TotalAmount,
                        ws.StatusLookupId,
                        ws.StatusLookup.Name,
                        ws.Notes,
                        ws.CreatedAt,
                        ws.Items.Select(i => new SupplyItemDto(
                            i.Id,
                            i.SparePartId,
                            i.SparePart.PartNumber,
                            i.SparePart.Name,
                            i.Quantity,
                            i.UnitPrice,
                            i.TotalPrice,
                            i.SerialNumber,
                            i.Notes)).ToList()))
                    .FirstOrDefaultAsync(ct);

                if (supply != null)
                    return supply;

                return null;
            }
        }

        return await _db.WarehouseSupplies
            .AsNoTracking()
            .Include(ws => ws.Station)
            .Include(ws => ws.StatusLookup)
            .Include(ws => ws.Items)
                .ThenInclude(i => i.SparePart)
            .Where(ws => ws.Id == id && ws.DeletedAt == null)
            .Select(ws => new WarehouseSupplyDetailDto(
                ws.Id,
                ws.SupplyNumber,
                ws.StationId,
                ws.Station.Name,
                ws.SupplyDate,
                ws.SupplierName,
                ws.InvoiceNumber,
                ws.InvoiceDate,
                ws.TotalAmount,
                ws.StatusLookupId,
                ws.StatusLookup.Name,
                ws.Notes,
                ws.CreatedAt,
                ws.Items.Select(i => new SupplyItemDto(
                    i.Id,
                    i.SparePartId,
                    i.SparePart.PartNumber,
                    i.SparePart.Name,
                    i.Quantity,
                    i.UnitPrice,
                    i.TotalPrice,
                    i.SerialNumber,
                    i.Notes)).ToList()))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<WarehouseSupplyListDto>> GetByStationAsync(int stationId, int page, int pageSize, CancellationToken ct = default)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleStationIds = await _scopeFilter.GetAccessibleStationIdsAsync(currentUserId.Value);
                if (!accessibleStationIds.Contains(stationId))
                    return new PagedResult<WarehouseSupplyListDto> { Items = Array.Empty<WarehouseSupplyListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
            }
        }

        var query = _db.WarehouseSupplies
            .AsNoTracking()
            .Include(ws => ws.Station)
            .Include(ws => ws.StatusLookup)
            .Include(ws => ws.Items)
            .Where(ws => ws.StationId == stationId && ws.DeletedAt == null);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(ws => ws.SupplyDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ws => new WarehouseSupplyListDto(
                ws.Id,
                ws.SupplyNumber,
                ws.Station.Name,
                ws.SupplyDate,
                ws.SupplierName,
                ws.InvoiceNumber,
                ws.TotalAmount,
                ws.StatusLookup.Name,
                ws.Items.Count,
                ws.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<WarehouseSupplyListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<WarehouseSupplyListDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.WarehouseSupplies
            .AsNoTracking()
            .Include(ws => ws.Station)
            .Include(ws => ws.StatusLookup)
            .Include(ws => ws.Items)
            .Where(ws => ws.DeletedAt == null)
            .AsQueryable();

        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleStationIds = await _scopeFilter.GetAccessibleStationIdsAsync(currentUserId.Value);
                if (accessibleStationIds.Count == 0)
                    return new PagedResult<WarehouseSupplyListDto> { Items = Array.Empty<WarehouseSupplyListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
                query = query.Where(ws => accessibleStationIds.Contains(ws.StationId));
            }
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(ws => ws.SupplyDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ws => new WarehouseSupplyListDto(
                ws.Id,
                ws.SupplyNumber,
                ws.Station.Name,
                ws.SupplyDate,
                ws.SupplierName,
                ws.InvoiceNumber,
                ws.TotalAmount,
                ws.StatusLookup.Name,
                ws.Items.Count,
                ws.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<WarehouseSupplyListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Result<WarehouseSupplyDetailDto>> CreateAsync(CreateWarehouseSupplyDto dto, int userId, CancellationToken ct = default)
    {
        var station = await _db.Stations.FirstOrDefaultAsync(s => s.Id == dto.StationId && s.DeletedAt == null, ct);
        if (station is null)
            return Result<WarehouseSupplyDetailDto>.NotFound("Station not found.");

        if (dto.Items == null || dto.Items.Count == 0)
            return Result<WarehouseSupplyDetailDto>.Failure("Supply must contain at least one item.");

        var supplyNumber = await GenerateSupplyNumberAsync(dto.StationId, ct);

        var statusDraft = await _db.Lookups
            .FirstOrDefaultAsync(l => l.Category.Name == "WarehouseSupplyStatus" && l.Code == "Draft", ct);

        var supply = new WarehouseSupply
        {
            SupplyNumber = supplyNumber,
            StationId = dto.StationId,
            SupplyDate = dto.SupplyDate,
            SupplierName = dto.SupplierName,
            InvoiceNumber = dto.InvoiceNumber,
            InvoiceDate = dto.InvoiceDate,
            TotalAmount = dto.TotalAmount,
            StatusLookupId = statusDraft != null ? statusDraft.Id : 0,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _db.WarehouseSupplies.Add(supply);
        await _db.SaveChangesAsync(ct);

        var items = new List<WarehouseSupplyItem>();
        foreach (var itemDto in dto.Items)
        {
            var sparePart = await _db.SpareParts.FirstOrDefaultAsync(sp => sp.Id == itemDto.SparePartId && sp.DeletedAt == null, ct);
            if (sparePart == null)
                return Result<WarehouseSupplyDetailDto>.Failure($"Spare part {itemDto.SparePartId} not found.");

            var totalPrice = itemDto.Quantity * itemDto.UnitPrice;
            items.Add(new WarehouseSupplyItem
            {
                WarehouseSupplyId = supply.Id,
                SparePartId = itemDto.SparePartId,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                TotalPrice = totalPrice,
                SerialNumber = itemDto.SerialNumber,
                Notes = itemDto.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            });
        }

        _db.WarehouseSupplyItems.AddRange(items);
        await _db.SaveChangesAsync(ct);

        var detail = await GetByIdAsync(supply.Id, ct);
        return Result<WarehouseSupplyDetailDto>.Success(detail!);
    }

    public async Task<Result<WarehouseSupplyDetailDto>> UpdateAsync(int id, UpdateWarehouseSupplyDto dto, int userId, CancellationToken ct = default)
    {
        var supply = await _db.WarehouseSupplies
            .Include(ws => ws.Items)
            .Include(ws => ws.StatusLookup)
            .FirstOrDefaultAsync(ws => ws.Id == id && ws.DeletedAt == null, ct);

        if (supply is null)
            return Result<WarehouseSupplyDetailDto>.NotFound($"Warehouse supply {id} not found.");

        if (supply.StatusLookup.Code == "Completed")
            return Result<WarehouseSupplyDetailDto>.Failure("Cannot update a completed supply.");

        var station = await _db.Stations.FirstOrDefaultAsync(s => s.Id == dto.StationId && s.DeletedAt == null, ct);
        if (station is null)
            return Result<WarehouseSupplyDetailDto>.NotFound("Station not found.");

        supply.StationId = dto.StationId;
        supply.SupplyDate = dto.SupplyDate;
        supply.SupplierName = dto.SupplierName;
        supply.InvoiceNumber = dto.InvoiceNumber;
        supply.InvoiceDate = dto.InvoiceDate;
        supply.TotalAmount = dto.TotalAmount;
        supply.Notes = dto.Notes;
        supply.UpdatedAt = DateTime.UtcNow;
        supply.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);

        if (dto.Items != null)
        {
            foreach (var existing in supply.Items)
            {
                _db.WarehouseSupplyItems.Remove(existing);
            }
            await _db.SaveChangesAsync(ct);

            var items = new List<WarehouseSupplyItem>();
            foreach (var itemDto in dto.Items)
            {
                var sparePart = await _db.SpareParts.FirstOrDefaultAsync(sp => sp.Id == itemDto.SparePartId && sp.DeletedAt == null, ct);
                if (sparePart == null)
                    return Result<WarehouseSupplyDetailDto>.Failure($"Spare part {itemDto.SparePartId} not found.");

                var totalPrice = itemDto.Quantity * itemDto.UnitPrice;
                items.Add(new WarehouseSupplyItem
                {
                    WarehouseSupplyId = supply.Id,
                    SparePartId = itemDto.SparePartId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    TotalPrice = totalPrice,
                    SerialNumber = itemDto.SerialNumber,
                    Notes = itemDto.Notes,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = userId
                });
            }

            _db.WarehouseSupplyItems.AddRange(items);
            await _db.SaveChangesAsync(ct);
        }

        var detail = await GetByIdAsync(supply.Id, ct);
        return Result<WarehouseSupplyDetailDto>.Success(detail!);
    }

    public async Task<Result<WarehouseSupplyDetailDto>> ConfirmAsync(int id, int userId, CancellationToken ct = default)
    {
        var supply = await _db.WarehouseSupplies
            .Include(ws => ws.Items)
            .FirstOrDefaultAsync(ws => ws.Id == id && ws.DeletedAt == null, ct);

        if (supply is null)
            return Result<WarehouseSupplyDetailDto>.NotFound($"Warehouse supply {id} not found.");

        if (supply.Items.Count == 0)
            return Result<WarehouseSupplyDetailDto>.Failure("Cannot confirm supply without items.");

        var completedStatus = await _db.Lookups
            .FirstOrDefaultAsync(l => l.Category.Name == "WarehouseSupplyStatus" && l.Code == "Completed", ct);

        supply.StatusLookupId = completedStatus != null ? completedStatus.Id : supply.StatusLookupId;
        supply.UpdatedAt = DateTime.UtcNow;
        supply.UpdatedBy = userId;

        foreach (var item in supply.Items)
        {
            var stock = await _db.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.SparePartId == item.SparePartId && ws.StationId == supply.StationId && ws.DeletedAt == null, ct);

            if (stock is null)
            {
                stock = new WarehouseStock
                {
                    SparePartId = item.SparePartId,
                    StationId = supply.StationId,
                    CurrentQuantity = item.Quantity,
                    ReservedQuantity = 0,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = userId
                };
                _db.WarehouseStocks.Add(stock);
            }
            else
            {
                stock.CurrentQuantity += item.Quantity;
                stock.UpdatedAt = DateTime.UtcNow;
                stock.UpdatedBy = userId;
            }

            var transactionTypeLookup = await _db.Lookups
                .FirstOrDefaultAsync(l => l.Category.Name == "WarehouseTransactionType" && l.Code == "Supply", ct);

            var transaction = new WarehouseTransaction
            {
                SparePartId = item.SparePartId,
                StationId = supply.StationId,
                TransactionTypeLookupId = transactionTypeLookup != null ? transactionTypeLookup.Id : 0,
                TransactionDate = DateTime.UtcNow,
                Quantity = item.Quantity,
                BalanceBefore = stock is null ? 0 : stock.CurrentQuantity - item.Quantity,
                BalanceAfter = stock is null ? item.Quantity : stock.CurrentQuantity,
                ReferenceId = supply.Id,
                ReferenceType = "WarehouseSupply",
                Notes = $"Supply {supply.SupplyNumber}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            };
            _db.WarehouseTransactions.Add(transaction);
        }

        await _db.SaveChangesAsync(ct);

        var detail = await GetByIdAsync(supply.Id, ct);
        return Result<WarehouseSupplyDetailDto>.Success(detail!);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var supply = await _db.WarehouseSupplies
            .Include(ws => ws.Items)
            .Include(ws => ws.StatusLookup)
            .FirstOrDefaultAsync(ws => ws.Id == id && ws.DeletedAt == null, ct);

        if (supply is null)
            return Result.NotFound($"Warehouse supply {id} not found.");

        if (supply.StatusLookup.Code == "Completed")
            return Result.Failure("Cannot delete a completed supply.");

        supply.IsActive = false;
        supply.DeletedAt = DateTime.UtcNow;
        supply.UpdatedAt = DateTime.UtcNow;
        supply.UpdatedBy = 0;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<string> GenerateSupplyNumberAsync(int stationId, CancellationToken ct)
    {
        var station = await _db.Stations.FirstOrDefaultAsync(s => s.Id == stationId && s.DeletedAt == null, ct);
        if (station == null)
            throw new InvalidOperationException("Station not found.");

        var year = DateTime.UtcNow.Year;
        var prefix = $"SUP-{year}-{station.Code}";

        var lastSupply = await _db.WarehouseSupplies
            .Where(ws => ws.SupplyNumber.StartsWith(prefix) && ws.DeletedAt == null)
            .OrderByDescending(ws => ws.SupplyNumber)
            .FirstOrDefaultAsync(ct);

        int seq = 1;
        if (lastSupply != null)
        {
            var parts = lastSupply.SupplyNumber.Split('-');
            if (parts.Length == 4 && int.TryParse(parts[3], out var lastSeq))
                seq = lastSeq + 1;
        }

        return $"{prefix}-{seq:D4}";
    }
}
