using FuelTrack.Application.DTOs.OilTransactions;
using FuelTrack.Application.Interfaces;
using FuelTrack.Application.Constants;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class OilTransactionService : IOilTransactionService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;
    private readonly ICommercialStationModeService _commercialMode;
    private readonly IAttachmentService _attachments;

    public OilTransactionService(
        FuelTrackDbContext db,
        IUserScopeFilterService scopeFilter,
        ICurrentUserService currentUser,
        ICommercialStationModeService commercialMode,
        IAttachmentService attachments)
    {
        _db = db;
        _currentUser = currentUser;
        _scopeFilter = scopeFilter;
        _commercialMode = commercialMode;
        _attachments = attachments;
    }

    private async Task<bool> CanAccessAsync(int subStationId)
    {
        if (await _commercialMode.IsCommercialModeAsync())
        {
            var scope = await _commercialMode.GetScopeAsync();
            return scope is not null && scope.SubStationId == subStationId;
        }
        if (_currentUser.HasAnyAdminRole) return true;
        return _currentUser.UserId is int userId && await _scopeFilter.HasAccessToSubStationAsync(userId, subStationId);
    }

    private async Task RequireAccessAsync(int subStationId)
    {
        if (!await CanAccessAsync(subStationId)) throw new UnauthorizedAccessException("Access denied to this sub-station.");
    }

    private async Task RequireTransferAccessAsync(int? sourceId, int? destinationId)
    {
        if (sourceId.HasValue) await RequireAccessAsync(sourceId.Value);
        if (destinationId.HasValue) await RequireAccessAsync(destinationId.Value);
    }

    public async Task<OilTransactionDetailDto> CreateAsync(CreateOilTransactionDto dto, int userId)
    {
        int targetSubStationId;
        var isCommercial = await _commercialMode.IsCommercialModeAsync();

        if (isCommercial)
        {
            var scope = await _commercialMode.GetScopeAsync();
            if (scope is null)
                throw new InvalidOperationException("Commercial mode has no active technical station scope.");
            targetSubStationId = scope.SubStationId;
        }
        else
        {
            var subStationExists = await _db.SubStations.AnyAsync(ss => ss.Id == dto.SubStationId);
            if (!subStationExists)
                throw new KeyNotFoundException($"SubStation {dto.SubStationId} not found.");
            targetSubStationId = dto.SubStationId;
        }

        await RequireAccessAsync(targetSubStationId);
        await RequireTransferAccessAsync(dto.SourceSubStationId, dto.DestinationSubStationId);

        if (isCommercial && dto.QuantityLiters <= 0)
            throw new ArgumentOutOfRangeException(nameof(dto.QuantityLiters), "Oil supply quantity must be greater than zero.");

        var attachmentIds = dto.AttachmentIds?.ToArray() ?? [];

        var oilLookupExists = await _db.Lookups.AnyAsync(l => l.Id == dto.OilLookupId);
        if (!oilLookupExists)
            throw new KeyNotFoundException($"Oil lookup {dto.OilLookupId} not found.");

        int txTypeId = dto.TransactionTypeLookupId;
        if (txTypeId <= 0)
        {
            var purchaseLookup = await _db.Lookups
                .FirstOrDefaultAsync(l => l.Category.Name == "TransactionType" && l.Code == "Purchase");
            if (purchaseLookup != null)
                txTypeId = purchaseLookup.Id;
        }

        var txTypeExists = await _db.Lookups.AnyAsync(l => l.Id == txTypeId);
        if (!txTypeExists)
            throw new KeyNotFoundException($"Transaction type lookup {txTypeId} not found.");

        await ValidateTransactionTypeAsync(txTypeId);

        var tx = new OilTransaction
        {
            SubStationId = targetSubStationId,
            OilLookupId = dto.OilLookupId,
            TransactionTypeLookupId = txTypeId,
            TransactionDate = dto.TransactionDate == default ? DateTime.UtcNow : dto.TransactionDate,
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

        try
        {
            await DbTransactionRunner.RunAsync(_db, async _ =>
            {
                // Keep ownership and unlinked checks in the same relational transaction as the row and links.
                await ValidateAttachmentIdsAsync(attachmentIds, userId);
                _db.OilTransactions.Add(tx);
                await _db.SaveChangesAsync();

                for (var sortOrder = 0; sortOrder < attachmentIds.Length; sortOrder++)
                {
                    var link = await _attachments.LinkAsync(
                        attachmentIds[sortOrder], AttachmentEntityTypes.OilTransaction, tx.Id, sortOrder, userId);
                    if (!link.IsSuccess)
                        throw new InvalidOperationException(link.Error ?? "Unable to link oil transaction attachment.");
                }
            });
        }
        catch
        {
            if (!_db.Database.IsRelational() && tx.Id > 0)
            {
                var linkedAttachments = await _db.AttachmentLinks
                    .Where(link => link.EntityType == AttachmentEntityTypes.OilTransaction && link.EntityId == tx.Id)
                    .ToListAsync();
                _db.AttachmentLinks.RemoveRange(linkedAttachments);
                _db.OilTransactions.Remove(tx);
                await _db.SaveChangesAsync();
            }
            throw;
        }

        return (await MapToDetailAsync(tx.Id))! with { AttachmentIds = attachmentIds };
    }

    public async Task<OilTransactionDetailDto> UpdateAsync(int id, UpdateOilTransactionDto dto, int userId)
    {
        var tx = await _db.OilTransactions.FindAsync(new object[] { id })
            ?? throw new KeyNotFoundException($"OilTransaction {id} not found.");

        await RequireAccessAsync(tx.SubStationId);
        await RequireTransferAccessAsync(dto.SourceSubStationId, dto.DestinationSubStationId);

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
        await RequireAccessAsync(tx.SubStationId);
        tx.UpdatedBy = userId;
        tx.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (await MapToDetailAsync(tx.Id))!;
    }

    public async Task<OilTransactionDetailDto> ApproveAsync(int id, int authorizedBy)
    {
        var tx = await _db.OilTransactions.FindAsync(id)
            ?? throw new KeyNotFoundException($"OilTransaction {id} not found");

        await RequireAccessAsync(tx.SubStationId);
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

        await RequireAccessAsync(transaction.SubStationId);
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
        var siteId = await _db.OilTransactions.Where(t => t.Id == id).Select(t => (int?)t.SubStationId).FirstOrDefaultAsync();
        if (!siteId.HasValue || !await CanAccessAsync(siteId.Value)) return null;
        var transaction = await MapToDetailAsync(id);
        if (transaction is null) return null;

        var attachmentIds = await _db.AttachmentLinks.AsNoTracking()
            .Where(link => link.EntityType == AttachmentEntityTypes.OilTransaction && link.EntityId == id)
            .OrderBy(link => link.SortOrder)
            .Select(link => link.AttachmentId)
            .ToListAsync();
        return transaction with { AttachmentIds = attachmentIds };
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

        if (await _commercialMode.IsCommercialModeAsync())
        {
            var scope = await _commercialMode.GetScopeAsync();
            if (scope is null)
                return new List<OilTransactionListDto>();
            query = query.Where(t => t.SubStationId == scope.SubStationId);
        }
        else if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (!userId.HasValue) return new List<OilTransactionListDto>();
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                query = query.Where(t => accessibleIds.Contains(t.SubStationId));
            }
        }

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new OilTransactionListDto(
                t.Id,
                t.SubStationId, t.SubStation.Name,
                t.OilLookupId, t.OilLookup.Name,
                t.TransactionTypeLookupId, t.TransactionTypeLookup.Name,
                t.TransactionDate, t.QuantityLiters,
                t.Reference,
                t.SourceSubStationId, t.SourceSubStation != null ? t.SourceSubStation.Name : null,
                t.DestinationSubStationId, t.DestinationSubStation != null ? t.DestinationSubStation.Name : null,
                t.IsApproved,
                t.ApprovedBy != null ? t.ApprovedBy.FullName : null,
                t.ApprovedAt,
                t.CreatedAt,
                t.CorrectedFromId,
                t.CorrectionReason))
            .ToListAsync();

        return await AddAttachmentIdsAsync(transactions);
    }

    public async Task<List<OilTransactionListDto>> GetBySubStationAsync(int subStationId)
    {
        await RequireAccessAsync(subStationId);

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

        if (await _commercialMode.IsCommercialModeAsync())
        {
            var scope = await _commercialMode.GetScopeAsync();
            if (scope is null) return new List<OilTransactionListDto>();
            query = query.Where(t => t.SubStationId == scope.SubStationId);
        }
        else if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (!userId.HasValue) return new List<OilTransactionListDto>();
            var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
            query = query.Where(t => accessibleIds.Contains(t.SubStationId));
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

        await RequireAccessAsync(generator.SubStationId);

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

    private async Task ValidateAttachmentIdsAsync(IReadOnlyList<int> attachmentIds, int userId)
    {
        if (attachmentIds.Count > 3)
            throw new InvalidOperationException("At most three attachments are allowed.");
        if (attachmentIds.Count != attachmentIds.Distinct().Count())
            throw new InvalidOperationException("Duplicate attachment identifiers are not allowed.");
        if (attachmentIds.Count == 0) return;

        var ownedIds = await _db.Attachments.AsNoTracking()
            .Where(x => attachmentIds.Contains(x.Id) && x.CreatedBy == userId)
            .Select(x => x.Id)
            .ToListAsync();
        if (ownedIds.Count != attachmentIds.Count)
            throw new UnauthorizedAccessException("Attachments must belong to the current user.");

        var isLinked = await _db.AttachmentLinks.AsNoTracking()
            .AnyAsync(x => attachmentIds.Contains(x.AttachmentId));
        if (isLinked)
            throw new InvalidOperationException("An attachment is already linked to another entity.");
    }

    private async Task<List<OilTransactionListDto>> AddAttachmentIdsAsync(List<OilTransactionListDto> transactions)
    {
        if (transactions.Count == 0) return transactions;

        var transactionIds = transactions.Select(x => x.Id).ToList();
        var links = await _db.AttachmentLinks.AsNoTracking()
            .Where(x => x.EntityType == AttachmentEntityTypes.OilTransaction && transactionIds.Contains(x.EntityId))
            .OrderBy(x => x.SortOrder)
            .Select(x => new { x.EntityId, x.AttachmentId })
            .ToListAsync();
        var attachmentIdsByTransaction = links.GroupBy(x => x.EntityId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<int>)x.Select(link => link.AttachmentId).ToList());

        return transactions.Select(transaction => transaction with
        {
            AttachmentIds = attachmentIdsByTransaction.TryGetValue(transaction.Id, out var attachmentIds)
                ? attachmentIds
                : Array.Empty<int>()
        }).ToList();
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
