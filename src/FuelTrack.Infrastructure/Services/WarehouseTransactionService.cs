using FuelTrack.Application;
using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class WarehouseTransactionService : IWarehouseTransactionService
{
    private readonly FuelTrackDbContext _db;

    public WarehouseTransactionService(FuelTrackDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<WarehouseTransactionDto>> GetByPartAsync(int sparePartId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.WarehouseTransactions
            .AsNoTracking()
            .Include(wt => wt.SparePart)
            .Include(wt => wt.Station)
            .Include(wt => wt.TransactionTypeLookup)
            .Where(wt => wt.SparePartId == sparePartId && wt.DeletedAt == null)
            .AsQueryable();

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(wt => wt.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(wt => new WarehouseTransactionDto(
                wt.Id,
                wt.SparePart.Name,
                wt.SparePart.PartNumber,
                wt.TransactionTypeLookup.Name,
                wt.TransactionDate,
                wt.Quantity,
                wt.BalanceBefore,
                wt.BalanceAfter,
                wt.ReferenceId,
                wt.ReferenceType,
                wt.Notes,
                "",
                wt.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<WarehouseTransactionDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<WarehouseTransactionDto>> GetByStationAsync(int stationId, DateTime startDate, DateTime endDate, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.WarehouseTransactions
            .AsNoTracking()
            .Include(wt => wt.SparePart)
            .Include(wt => wt.Station)
            .Include(wt => wt.TransactionTypeLookup)
            .Where(wt => wt.StationId == stationId && wt.DeletedAt == null && wt.TransactionDate >= startDate && wt.TransactionDate <= endDate)
            .AsQueryable();

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(wt => wt.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(wt => new WarehouseTransactionDto(
                wt.Id,
                wt.SparePart.Name,
                wt.SparePart.PartNumber,
                wt.TransactionTypeLookup.Name,
                wt.TransactionDate,
                wt.Quantity,
                wt.BalanceBefore,
                wt.BalanceAfter,
                wt.ReferenceId,
                wt.ReferenceType,
                wt.Notes,
                "",
                wt.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<WarehouseTransactionDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
