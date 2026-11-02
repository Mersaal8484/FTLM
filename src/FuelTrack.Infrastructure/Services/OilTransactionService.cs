using FuelTrack.Application.DTOs.OilTransactions;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class OilTransactionService : IOilTransactionService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;

    public OilTransactionService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
        _scopeFilter = scopeFilter;
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

        await ValidateTransactionTypeAsync(dto.TransactionTypeLookupId);

        var tx = new OilTransaction
        {
            SubStationId = dto.SubStationId,
            OilLookupId = dto.OilLookupId,
            TransactionTypeLookupId = dto.TransactionTypeLookupId,
            TransactionDate = dto.TransactionDate,
            QuantityLiters = dto.QuantityLiters,
            Reference = dto.Reference,
            SupplierCompany = dto.SupplierCompany,
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
        if (dto.TransactionDate.HasValue) tx.TransactionDate = dto.TransactionDate.Value;
        if (dto.QuantityLiters.HasValue) tx.QuantityLiters = dto.QuantityLiters.Value;
        if (dto.SupplierCompany is not null) tx.SupplierCompany = dto.SupplierCompany;
        if (dto.Reference is not null) tx.Reference = dto.Reference;
        if (dto.SourceSubStationId.HasValue) tx.SourceSubStationId = dto.SourceSubStationId.Value;
        if (dto.DestinationSubStationId.HasValue) tx.DestinationSubStationId = dto.DestinationSubStationId.Value;
        if (dto.Notes is not null) tx.Notes = dto.Notes;

        tx.UpdatedAt = DateTime.UtcNow;
        tx.UpdatedBy = userId;

        await _db.SaveChangesAsync();

        return (await MapToDetailAsync(tx.Id))!;
    }

    public async Task<OilTransactionDetailDto> SubmitForApprovalAsync(int id, int userId)
    {
        var tx = await _db.OilTransactions.FindAsync(id)
            ?? throw new KeyNotFoundException($"OilTransaction {id} not found");
        tx.UpdatedBy = userId;
        tx.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (await MapToDetailAsync(tx.Id))!;
    }

    public async Task<OilTransactionDetailDto> ApproveAsync(int id, int authorizedBy)
    {
        var tx = await _db.OilTransactions.FindAsync(id)
            ?? throw new KeyNotFoundException($"OilTransaction {id} not found");

        tx.IsApproved = true;
        tx.ApprovedById = authorizedBy;
        tx.ApprovedAt = DateTime.UtcNow;
        tx.UpdatedAt = DateTime.UtcNow;
        tx.UpdatedBy = authorizedBy;

        await _db.SaveChangesAsync();
        return (await MapToDetailAsync(tx.Id))!;
    }

    public async Task<OilTransactionDetailDto> RejectAsync(int id, int rejectedById, string? rejectionReason = null)
    {
        var transaction = await _db.OilTransactions.FindAsync(id)
            ?? throw new KeyNotFoundException($"OilTransaction {id} not found");

        transaction.IsApproved = false;
        transaction.ApprovedById = rejectedById;
        transaction.ApprovedAt = DateTime.UtcNow;
        transaction.Notes = string.IsNullOrEmpty(rejectionReason) ? transaction.Notes : $"{transaction.Notes}\nRejection Reason: {rejectionReason}".Trim();
        transaction.UpdatedAt = DateTime.UtcNow;
        transaction.UpdatedBy = rejectedById;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id) ?? throw new KeyNotFoundException();
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
            .Include(t => t.SourceSubStation)
            .Include(t => t.DestinationSubStation)
            .AsQueryable();

        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
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
        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (userId.HasValue && !await _scopeFilter.HasAccessToSubStationAsync(userId.Value, subStationId))
                throw new UnauthorizedAccessException("Access denied to this sub-station.");
        }

        return await _db.OilTransactions
            .Include(t => t.SubStation)
            .Include(t => t.OilLookup)
            .Include(t => t.TransactionTypeLookup)
            .Include(t => t.ApprovedBy)
            .Include(t => t.SourceSubStation)
            .Include(t => t.DestinationSubStation)
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
            .Include(t => t.SourceSubStation)
            .Include(t => t.DestinationSubStation)
            .Where(t => t.SubStation != null && t.SubStation.StationId == stationId)
            .AsQueryable();

        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
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

        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (userId.HasValue && !await _scopeFilter.HasAccessToSubStationAsync(userId.Value, generator.SubStationId))
                throw new UnauthorizedAccessException("Access denied to this generator's sub-station.");
        }

        return await _db.OilTransactions
            .Include(t => t.SubStation)
            .Include(t => t.OilLookup)
            .Include(t => t.TransactionTypeLookup)
            .Include(t => t.ApprovedBy)
            .Include(t => t.SourceSubStation)
            .Include(t => t.DestinationSubStation)
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
            .Include(t => t.ApprovedBy)
            .Include(t => t.SourceSubStation)
            .Include(t => t.DestinationSubStation)
            .Where(t => t.Id == id)
            .Select(t => new OilTransactionDetailDto(
                t.Id,
                t.SubStationId, t.SubStation.Name,
                t.OilLookupId, t.OilLookup.Name,
                t.TransactionTypeLookupId, t.TransactionTypeLookup.Name,
                t.TransactionDate, t.QuantityLiters,
                t.Reference,
                t.SupplierCompany,
                t.SourceSubStationId, t.SourceSubStation != null ? t.SourceSubStation.Name : null,
                t.DestinationSubStationId, t.DestinationSubStation != null ? t.DestinationSubStation.Name : null,
                t.IsApproved,
                t.ApprovedById, t.ApprovedBy != null ? t.ApprovedBy.FullName : null,
                t.ApprovedAt,
                t.Notes,
                t.CorrectedFromId,
                t.CorrectionReason,
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
            t.TransactionTypeLookupId, t.TransactionTypeLookup.Name,
            t.TransactionDate, t.QuantityLiters,
            t.Reference,
            t.SourceSubStationId, t.SourceSubStation != null ? t.SourceSubStation.Name : null,
            t.DestinationSubStationId, t.DestinationSubStation != null ? t.DestinationSubStation.Name : null,
            t.IsApproved,
            t.ApprovedBy?.FullName,
            t.ApprovedAt,
            t.CreatedAt,
            t.CorrectedFromId,
            t.CorrectionReason);
    }

    private static readonly HashSet<string> AllowedTransactionTypeCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Purchase", "Transfer", "Loss", "Consumption"
    };

    private async Task ValidateTransactionTypeAsync(int transactionTypeLookupId)
    {
        var code = await _db.Lookups
            .Where(l => l.Id == transactionTypeLookupId)
            .Select(l => l.Code)
            .FirstOrDefaultAsync();

        if (code is null || !AllowedTransactionTypeCodes.Contains(code))
        {
            var allowed = string.Join(", ", AllowedTransactionTypeCodes);
            throw new InvalidOperationException(
                $"Invalid transaction type ID {transactionTypeLookupId}. Allowed types: {allowed}.");
        }
    }
}
