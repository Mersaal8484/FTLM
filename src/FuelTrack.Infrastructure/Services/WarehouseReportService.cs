using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class WarehouseReportService : IWarehouseReportService
{
    private readonly FuelTrackDbContext _db;

    public WarehouseReportService(FuelTrackDbContext db)
    {
        _db = db;
    }

    public async Task<List<WarehouseStockDto>> GetStockReportAsync(int? stationId, CancellationToken ct = default)
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
            .Select(ws => new WarehouseStockDto(
                ws.SparePartId,
                ws.SparePart.PartNumber,
                ws.SparePart.Name,
                ws.SparePart.PartCategoryLookup != null ? ws.SparePart.PartCategoryLookup.Name : string.Empty,
                ws.SparePart.UnitLookup != null ? ws.SparePart.UnitLookup.Name : string.Empty,
                ws.StationId,
                ws.Station.Name,
                ws.CurrentQuantity,
                ws.ReservedQuantity,
                ws.AvailableQuantity,
                ws.SparePart.MinStockQuantity,
                ws.CurrentQuantity < ws.SparePart.MinStockQuantity))
            .ToListAsync(ct);
    }

    public async Task<WarehouseReportDto> GetMovementReportAsync(int stationId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var transactions = await _db.WarehouseTransactions
            .AsNoTracking()
            .Include(wt => wt.SparePart)
            .Include(wt => wt.Station)
            .Include(wt => wt.TransactionTypeLookup)
            .Where(wt => wt.StationId == stationId && wt.DeletedAt == null && wt.TransactionDate >= startDate && wt.TransactionDate <= endDate)
            .ToListAsync(ct);

        var totalSupplied = transactions
            .Where(wt => wt.TransactionTypeLookup.Code == "Supply")
            .Sum(wt => wt.Quantity);

        var totalDispatched = transactions
            .Where(wt => wt.TransactionTypeLookup.Code == "Dispatch")
            .Sum(wt => wt.Quantity);

        var station = await _db.Stations.FirstOrDefaultAsync(s => s.Id == stationId && s.DeletedAt == null, ct);

        var lowStockItems = await _db.WarehouseStocks
            .AsNoTracking()
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.PartCategoryLookup)
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.UnitLookup)
            .Where(ws => ws.StationId == stationId && ws.DeletedAt == null && ws.CurrentQuantity < ws.SparePart.MinStockQuantity)
            .Select(ws => new SparePartListDto(
                ws.SparePart.Id,
                ws.SparePart.PartNumber,
                ws.SparePart.Name,
                ws.SparePart.PartCategoryLookup.Name,
                ws.SparePart.UnitLookup.Name,
                ws.SparePart.MinStockQuantity,
                ws.SparePart.ReplacementIntervalHours,
                ws.SparePart.IsHoursTracked,
                ws.SparePart.UnitPrice,
                ws.SparePart.Notes,
                ws.SparePart.IsActive))
            .ToListAsync(ct);

        var transactionDtos = transactions.Select(wt => new WarehouseTransactionDto(
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
            wt.CreatedAt)).ToList();

        return new WarehouseReportDto(
            stationId,
            station != null ? station.Name : "",
            startDate,
            endDate,
            totalSupplied,
            totalDispatched,
            totalSupplied - totalDispatched,
            lowStockItems,
            transactionDtos);
    }

    public async Task<List<WarehouseStockDto>> GetLowStockAlertAsync(CancellationToken ct = default)
    {
        return await _db.WarehouseStocks
            .AsNoTracking()
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.PartCategoryLookup)
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.UnitLookup)
            .Include(ws => ws.Station)
            .Where(ws => ws.DeletedAt == null && ws.CurrentQuantity < ws.SparePart.MinStockQuantity)
            .Select(ws => new WarehouseStockDto(
                ws.SparePartId,
                ws.SparePart.PartNumber,
                ws.SparePart.Name,
                ws.SparePart.PartCategoryLookup != null ? ws.SparePart.PartCategoryLookup.Name : string.Empty,
                ws.SparePart.UnitLookup != null ? ws.SparePart.UnitLookup.Name : string.Empty,
                ws.StationId,
                ws.Station.Name,
                ws.CurrentQuantity,
                ws.ReservedQuantity,
                ws.AvailableQuantity,
                ws.SparePart.MinStockQuantity,
                true))
            .ToListAsync(ct);
    }

    public async Task<List<WarehouseTransactionDto>> GetDispatchByGeneratorAsync(int generatorId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        var subStationId = await _db.Generators
            .Where(g => g.Id == generatorId && g.DeletedAt == null)
            .Select(g => g.SubStationId)
            .FirstOrDefaultAsync(ct);

        if (subStationId == 0)
            return new List<WarehouseTransactionDto>();

        var dispatchRequests = await _db.DispatchRequests
            .Where(dr => dr.SubStationId == subStationId && dr.DeletedAt == null)
            .Select(dr => dr.Id)
            .ToListAsync(ct);

        var query = _db.WarehouseTransactions
            .AsNoTracking()
            .Include(wt => wt.SparePart)
            .Include(wt => wt.Station)
            .Include(wt => wt.TransactionTypeLookup)
            .Where(wt => wt.ReferenceType == "DispatchRequest" && dispatchRequests.Contains(wt.ReferenceId ?? 0) && wt.DeletedAt == null)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(wt => wt.TransactionDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(wt => wt.TransactionDate <= endDate.Value);

        return await query
            .OrderByDescending(wt => wt.TransactionDate)
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
    }

    public async Task<List<WarehouseTransactionDto>> GetDispatchBySubStationAsync(int subStationId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        var dispatchRequests = await _db.DispatchRequests
            .Where(dr => dr.SubStationId == subStationId && dr.DeletedAt == null)
            .Select(dr => dr.Id)
            .ToListAsync(ct);

        var query = _db.WarehouseTransactions
            .AsNoTracking()
            .Include(wt => wt.SparePart)
            .Include(wt => wt.Station)
            .Include(wt => wt.TransactionTypeLookup)
            .Where(wt => wt.ReferenceType == "DispatchRequest" && dispatchRequests.Contains(wt.ReferenceId ?? 0) && wt.DeletedAt == null)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(wt => wt.TransactionDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(wt => wt.TransactionDate <= endDate.Value);

        return await query
            .OrderByDescending(wt => wt.TransactionDate)
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
    }

    public async Task<List<WarehouseSupplyListDto>> GetSupplyReportAsync(int stationId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        return await _db.WarehouseSupplies
            .AsNoTracking()
            .Include(ws => ws.Station)
            .Include(ws => ws.StatusLookup)
            .Include(ws => ws.Items)
            .Where(ws => ws.StationId == stationId && ws.DeletedAt == null && ws.SupplyDate >= startDate && ws.SupplyDate <= endDate)
            .OrderByDescending(ws => ws.SupplyDate)
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
    }

    public async Task<WarehouseReportDto> GetPeriodicInventoryReportAsync(int stationId, string type, int year, int period, CancellationToken ct = default)
    {
        var station = await _db.Stations.FirstOrDefaultAsync(s => s.Id == stationId && s.DeletedAt == null, ct);
        if (station == null)
            return new WarehouseReportDto(stationId, "", DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0, new List<SparePartListDto>(), new List<WarehouseTransactionDto>());

        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31);

        if (type == "FirstHalf")
            endDate = new DateTime(year, 6, 30);
        else if (type == "SecondHalf")
            startDate = new DateTime(year, 7, 1);

        var transactions = await _db.WarehouseTransactions
            .AsNoTracking()
            .Include(wt => wt.SparePart)
            .Include(wt => wt.Station)
            .Include(wt => wt.TransactionTypeLookup)
            .Where(wt => wt.StationId == stationId && wt.DeletedAt == null && wt.TransactionDate >= startDate && wt.TransactionDate <= endDate)
            .ToListAsync(ct);

        var totalSupplied = transactions
            .Where(wt => wt.TransactionTypeLookup.Code == "Supply")
            .Sum(wt => wt.Quantity);

        var totalDispatched = transactions
            .Where(wt => wt.TransactionTypeLookup.Code == "Dispatch")
            .Sum(wt => wt.Quantity);

        var lowStockItems = await _db.WarehouseStocks
            .AsNoTracking()
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.PartCategoryLookup)
            .Include(ws => ws.SparePart)
                .ThenInclude(sp => sp.UnitLookup)
            .Where(ws => ws.StationId == stationId && ws.DeletedAt == null && ws.CurrentQuantity < ws.SparePart.MinStockQuantity)
            .Select(ws => new SparePartListDto(
                ws.SparePart.Id,
                ws.SparePart.PartNumber,
                ws.SparePart.Name,
                ws.SparePart.PartCategoryLookup.Name,
                ws.SparePart.UnitLookup.Name,
                ws.SparePart.MinStockQuantity,
                ws.SparePart.ReplacementIntervalHours,
                ws.SparePart.IsHoursTracked,
                ws.SparePart.UnitPrice,
                ws.SparePart.Notes,
                ws.SparePart.IsActive))
            .ToListAsync(ct);

        var transactionDtos = transactions.Select(wt => new WarehouseTransactionDto(
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
            wt.CreatedAt)).ToList();

        return new WarehouseReportDto(
            stationId,
            station.Name,
            startDate,
            endDate,
            totalSupplied,
            totalDispatched,
            totalSupplied - totalDispatched,
            lowStockItems,
            transactionDtos);
    }
}
