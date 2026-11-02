using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class WarehouseStockService : IWarehouseStockService
{
    private readonly FuelTrackDbContext _db;

    public WarehouseStockService(FuelTrackDbContext db)
    {
        _db = db;
    }

    public async Task<List<WarehouseStockDto>> GetByStationAsync(int stationId, CancellationToken ct = default)
    {
        return await _db.WarehouseStocks
            .AsNoTracking()
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.PartCategoryLookup)
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.UnitLookup)
            .Include(ws => ws.Station)
            .Where(ws => ws.StationId == stationId && ws.DeletedAt == null)
            .Select(ws => MapToDto(ws))
            .ToListAsync(ct);
    }

    public async Task<List<WarehouseStockDto>> GetAllAsync(int? stationId = null, CancellationToken ct = default)
    {
        var query = _db.WarehouseStocks
            .AsNoTracking()
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.PartCategoryLookup)
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.UnitLookup)
            .Include(ws => ws.Station)
            .Where(ws => ws.DeletedAt == null)
            .AsQueryable();

        if (stationId.HasValue)
            query = query.Where(ws => ws.StationId == stationId.Value);

        return await query
            .Select(ws => MapToDto(ws))
            .ToListAsync(ct);
    }

    public async Task<List<WarehouseStockDto>> GetByPartAsync(int sparePartId, CancellationToken ct = default)
    {
        return await _db.WarehouseStocks
            .AsNoTracking()
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.PartCategoryLookup)
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.UnitLookup)
            .Include(ws => ws.Station)
            .Where(ws => ws.SparePartId == sparePartId && ws.DeletedAt == null)
            .Select(ws => MapToDto(ws))
            .ToListAsync(ct);
    }

    public async Task<List<WarehouseStockDto>> GetLowStockAsync(int? stationId, CancellationToken ct = default)
    {
        var query = _db.WarehouseStocks
            .AsNoTracking()
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.PartCategoryLookup)
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.UnitLookup)
            .Include(ws => ws.Station)
            .Where(ws => ws.DeletedAt == null && ws.CurrentQuantity < ws.SparePart.MinStockQuantity)
            .AsQueryable();

        if (stationId.HasValue)
            query = query.Where(ws => ws.StationId == stationId.Value);

        return await query
            .Select(ws => MapToDto(ws))
            .ToListAsync(ct);
    }

    public async Task AdjustStockAsync(int sparePartId, int stationId, decimal delta, string reason, int userId, CancellationToken ct = default)
    {
        var stock = await _db.WarehouseStocks
            .FirstOrDefaultAsync(ws => ws.SparePartId == sparePartId && ws.StationId == stationId && ws.DeletedAt == null, ct);

        if (stock is null)
        {
            stock = new WarehouseStock
            {
                SparePartId = sparePartId,
                StationId = stationId,
                CurrentQuantity = 0,
                ReservedQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            };
            _db.WarehouseStocks.Add(stock);
        }

        var balanceBefore = stock.CurrentQuantity;
        stock.CurrentQuantity += delta;
        if (stock.CurrentQuantity < 0)
            stock.CurrentQuantity = 0;

        stock.UpdatedAt = DateTime.UtcNow;
        stock.UpdatedBy = userId;

        var transactionType = delta >= 0 ? "Supply" : "Dispatch";
        var transactionTypeLookup = await _db.Lookups
            .FirstOrDefaultAsync(l => l.Category.Name == "WarehouseTransactionType" && l.Code == transactionType, ct);

        var transaction = new WarehouseTransaction
        {
            SparePartId = sparePartId,
            StationId = stationId,
            TransactionTypeLookupId = transactionTypeLookup != null ? transactionTypeLookup.Id : 0,
            TransactionDate = DateTime.UtcNow,
            Quantity = Math.Abs(delta),
            BalanceBefore = balanceBefore,
            BalanceAfter = stock.CurrentQuantity,
            ReferenceType = "Adjustment",
            Notes = reason,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _db.WarehouseTransactions.Add(transaction);
        await _db.SaveChangesAsync(ct);
    }

    private static WarehouseStockDto MapToDto(WarehouseStock ws)
    {
        return new WarehouseStockDto(
            ws.SparePartId,
            ws.SparePart.PartNumber,
            ws.SparePart.Name,
            ws.SparePart.PartCategoryLookup?.Name ?? string.Empty,
            ws.SparePart.UnitLookup?.Name ?? string.Empty,
            ws.StationId,
            ws.Station.Name,
            ws.CurrentQuantity,
            ws.ReservedQuantity,
            ws.AvailableQuantity,
            ws.SparePart.MinStockQuantity,
            ws.CurrentQuantity < ws.SparePart.MinStockQuantity);
    }
}
