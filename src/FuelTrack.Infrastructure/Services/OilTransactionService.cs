using System.Security.Claims;
using FuelTrack.Application.DTOs.OilTransactions;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class OilTransactionService : IOilTransactionService
{
    private readonly FuelTrackDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserScopeFilterService _scopeFilter;

    public OilTransactionService(FuelTrackDbContext db, IHttpContextAccessor httpContextAccessor, IUserScopeFilterService scopeFilter)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _scopeFilter = scopeFilter;
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

    public async Task<OilTransactionDetailDto> CreateAsync(CreateOilTransactionDto dto, int userId)
    {
        var subStationExists = await _db.SubStations.AnyAsync(ss => ss.Id == dto.SubStationId);
        if (!subStationExists)
            throw new KeyNotFoundException($"SubStation {dto.SubStationId} not found.");

        var oilLookupExists = await _db.Lookups.AnyAsync(l => l.Id == dto.OilLookupId);
        if (!oilLookupExists)
            throw new KeyNotFoundException($"Oil lookup {dto.OilLookupId} not found.");

        var txTypeExists = await _db.Lookups.AnyAsync(l => l.Id == dto.TransactionTypeLookupId);
        if (!txTypeExists)
            throw new KeyNotFoundException($"Transaction type lookup {dto.TransactionTypeLookupId} not found.");

        var tx = new OilTransaction
        {
            SubStationId = dto.SubStationId,
            OilLookupId = dto.OilLookupId,
            OilId = dto.OilId,
            TransactionTypeLookupId = dto.TransactionTypeLookupId,
            TransactionDate = dto.TransactionDate,
            QuantityLiters = dto.QuantityLiters,
            SupplierCompany = dto.SupplierCompany,
            DriverName = dto.DriverName,
            DriverIdCard = dto.DriverIdCard,
            TruckNumber = dto.TruckNumber,
            InvoiceNumber = dto.InvoiceNumber,
            InvoiceDate = dto.InvoiceDate,
            InvoiceQuantity = dto.InvoiceQuantity,
            WarehouseReceiptNumber = dto.WarehouseReceiptNumber,
            WarehouseReceiptDate = dto.WarehouseReceiptDate,
            WarehouseQuantity = dto.WarehouseQuantity,
            SourceSubStationId = dto.SourceSubStationId,
            DestinationSubStationId = dto.DestinationSubStationId,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
        };

        _db.OilTransactions.Add(tx);
        await _db.SaveChangesAsync();

        return (await MapToDetailAsync(tx.Id))!;
    }

    public async Task<OilTransactionDetailDto> UpdateAsync(int id, UpdateOilTransactionDto dto, int userId)
    {
        var tx = await _db.OilTransactions.FindAsync(new object[] { id })
            ?? throw new KeyNotFoundException($"OilTransaction {id} not found.");

        if (dto.OilLookupId.HasValue) tx.OilLookupId = dto.OilLookupId.Value;
        if (dto.OilId.HasValue) tx.OilId = dto.OilId;
        if (dto.TransactionTypeLookupId.HasValue) tx.TransactionTypeLookupId = dto.TransactionTypeLookupId.Value;
        if (dto.TransactionDate.HasValue) tx.TransactionDate = dto.TransactionDate.Value;
        if (dto.QuantityLiters.HasValue) tx.QuantityLiters = dto.QuantityLiters.Value;
        if (dto.SupplierCompany is not null) tx.SupplierCompany = dto.SupplierCompany;
        if (dto.DriverName is not null) tx.DriverName = dto.DriverName;
        if (dto.DriverIdCard is not null) tx.DriverIdCard = dto.DriverIdCard;
        if (dto.TruckNumber is not null) tx.TruckNumber = dto.TruckNumber;
        if (dto.InvoiceNumber is not null) tx.InvoiceNumber = dto.InvoiceNumber;
        if (dto.InvoiceDate.HasValue) tx.InvoiceDate = dto.InvoiceDate;
        if (dto.InvoiceQuantity.HasValue) tx.InvoiceQuantity = dto.InvoiceQuantity;
        if (dto.WarehouseReceiptNumber is not null) tx.WarehouseReceiptNumber = dto.WarehouseReceiptNumber;
        if (dto.WarehouseReceiptDate.HasValue) tx.WarehouseReceiptDate = dto.WarehouseReceiptDate;
        if (dto.WarehouseQuantity.HasValue) tx.WarehouseQuantity = dto.WarehouseQuantity;
        if (dto.SourceSubStationId.HasValue) tx.SourceSubStationId = dto.SourceSubStationId;
        if (dto.DestinationSubStationId.HasValue) tx.DestinationSubStationId = dto.DestinationSubStationId;
        if (dto.Notes is not null) tx.Notes = dto.Notes;

        tx.UpdatedAt = DateTime.UtcNow;
        tx.UpdatedBy = userId;

        await _db.SaveChangesAsync();

        return (await MapToDetailAsync(tx.Id))!;
    }

    public async Task<OilTransactionDetailDto?> GetByIdAsync(int id)
    {
        return await MapToDetailAsync(id);
    }

    public async Task<List<OilTransactionListDto>> GetAllAsync()
    {
        var query = _db.OilTransactions
            .Include(t => t.SubStation)
            .Include(t => t.OilLookup)
            .Include(t => t.TransactionTypeLookup)
            .Include(t => t.ApprovedBy)
            .Include(t => t.Oil)
            .AsQueryable();

        if (!IsAdmin())
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                query = query.Where(t => accessibleIds.Contains(t.SubStationId));
            }
        }

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => MapToList(t))
            .ToListAsync();
    }

    public async Task<List<OilTransactionListDto>> GetBySubStationAsync(int subStationId)
    {
        if (!IsAdmin())
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue && !await _scopeFilter.HasAccessToSubStationAsync(userId.Value, subStationId))
                throw new UnauthorizedAccessException("Access denied to this sub-station.");
        }

        return await _db.OilTransactions
            .Include(t => t.SubStation)
            .Include(t => t.OilLookup)
            .Include(t => t.TransactionTypeLookup)
            .Include(t => t.ApprovedBy)
            .Include(t => t.Oil)
            .Where(t => t.SubStationId == subStationId)
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => MapToList(t))
            .ToListAsync();
    }

    public async Task<List<OilTransactionListDto>> GetByStationAsync(int stationId)
    {
        var query = _db.OilTransactions
            .Include(t => t.SubStation)
            .Include(t => t.OilLookup)
            .Include(t => t.TransactionTypeLookup)
            .Include(t => t.ApprovedBy)
            .Include(t => t.Oil)
            .Where(t => t.SubStation != null && t.SubStation.StationId == stationId)
            .AsQueryable();

        if (!IsAdmin())
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                query = query.Where(t => accessibleIds.Contains(t.SubStationId));
            }
        }

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => MapToList(t))
            .ToListAsync();
    }

    public async Task<List<OilTransactionListDto>> GetByGeneratorAsync(int generatorId)
    {
        var generator = await _db.Generators.FindAsync(generatorId);
        if (generator == null) return new List<OilTransactionListDto>();

        if (!IsAdmin())
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue && !await _scopeFilter.HasAccessToSubStationAsync(userId.Value, generator.SubStationId))
                throw new UnauthorizedAccessException("Access denied to this generator's sub-station.");
        }

        return await _db.OilTransactions
            .Include(t => t.SubStation)
            .Include(t => t.OilLookup)
            .Include(t => t.TransactionTypeLookup)
            .Include(t => t.ApprovedBy)
            .Include(t => t.Oil)
            .Where(t => t.SubStationId == generator.SubStationId)
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => MapToList(t))
            .ToListAsync();
    }

    private async Task<OilTransactionDetailDto?> MapToDetailAsync(int id)
    {
        return await _db.OilTransactions
            .Include(t => t.SubStation)
            .Include(t => t.OilLookup)
            .Include(t => t.TransactionTypeLookup)
            .Include(t => t.SourceSubStation)
            .Include(t => t.DestinationSubStation)
            .Include(t => t.ApprovedBy)
            .Include(t => t.Oil)
            .Where(t => t.Id == id)
            .Select(t => new OilTransactionDetailDto(
                t.Id,
                t.SubStationId, t.SubStation.Name,
                t.OilLookupId, t.OilLookup.Name,
                t.OilId, t.Oil != null ? t.Oil.OilLookup!.Name : null,
                t.TransactionTypeLookupId, t.TransactionTypeLookup.Name,
                t.TransactionDate, t.QuantityLiters,
                t.SupplierCompany, t.DriverName, t.DriverIdCard, t.TruckNumber,
                t.InvoiceNumber, t.InvoiceDate, t.InvoiceQuantity,
                t.WarehouseReceiptNumber, t.WarehouseReceiptDate, t.WarehouseQuantity,
                t.SourceSubStationId, t.SourceSubStation != null ? t.SourceSubStation.Name : null,
                t.DestinationSubStationId, t.DestinationSubStation != null ? t.DestinationSubStation.Name : null,
                t.IsApproved,
                t.ApprovedById, t.ApprovedBy != null ? t.ApprovedBy.FullName : null,
                t.ApprovedAt,
                t.Notes,
                t.CreatedBy, t.UpdatedBy,
                t.CreatedAt, t.UpdatedAt))
            .FirstOrDefaultAsync();
    }

    private static OilTransactionListDto MapToList(OilTransaction t)
    {
        return new OilTransactionListDto(
            t.Id,
            t.SubStationId, t.SubStation.Name,
            t.OilLookupId, t.OilLookup.Name,
            t.OilId, t.Oil?.OilLookup?.Name,
            t.TransactionTypeLookupId, t.TransactionTypeLookup.Name,
            t.TransactionDate, t.QuantityLiters,
            t.InvoiceNumber,
            t.IsApproved,
            t.ApprovedBy?.FullName,
            t.ApprovedAt,
            t.CreatedAt);
    }
}
