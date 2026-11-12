using FuelTrack.Application;
using FuelTrack.Application.DTOs.FuelMovements;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class FuelMovementService : IFuelMovementService
{
    private readonly FuelTrackDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserScopeFilterService _scopeFilter;

    public FuelMovementService(FuelTrackDbContext db, ICurrentUserService currentUser, IUserScopeFilterService scopeFilter)
    {
        _db = db;
        _currentUser = currentUser;
        _scopeFilter = scopeFilter;
    }

    public async Task<FuelMovementDetailDto> CreateAsync(CreateFuelMovementDto dto, int userId)
    {
        await ValidateTransactionTypeAsync(dto.TransactionTypeLookupId);

        var transactionTypeCode = await GetTransactionTypeCodeAsync(dto.TransactionTypeLookupId);
        if (IsInboundTransaction(transactionTypeCode))
        {
            await ValidateInboundCapacityAsync(dto.SubStationId, dto.QuantityLiters, dto.FuelLookupId);
        }

        var movement = new FuelTransaction
        {
            SubStationId = dto.SubStationId,
            FuelLookupId = dto.FuelLookupId,
            TransactionTypeLookupId = dto.TransactionTypeLookupId,
            TransactionDate = dto.TransactionDate,
            QuantityLiters = dto.QuantityLiters,
            Reference = dto.Reference,
            SupplierCompany = dto.SupplierCompany,
            DriverName = dto.DriverName,
            DriverIdCard = dto.DriverIdCard,
            TruckNumber = dto.TruckNumber,
            InvoiceNumber = dto.InvoiceNumber,
            InvoiceDate = dto.InvoiceDate,
            InvoiceQuantity = dto.InvoiceQuantity,
            MeterId = dto.MeterId,
            PrevMeterVisualReading = dto.PrevMeterVisualReading,
            CurrentMeterVisualReading = dto.CurrentMeterVisualReading,
            PrevMeterStandardReading = dto.PrevMeterStandardReading,
            CurrentMeterStandardReading = dto.CurrentMeterStandardReading,
            SourceSubStationId = dto.SourceSubStationId,
            DestinationSubStationId = dto.DestinationSubStationId,
            CorrectedFromId = dto.CorrectedFromId,
            CorrectionReason = dto.CorrectionReason,
            IsApproved = false,
            CreatedBy = userId,
            UpdatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Notes = dto.Notes
        };

        _db.FuelTransactions.Add(movement);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(movement.Id)
            ?? throw new InvalidOperationException("Failed to retrieve created transaction.");
    }

    public async Task<FuelMovementDetailDto> UpdateAsync(int id, UpdateFuelMovementDto dto, int userId)
    {
        var movement = await _db.FuelTransactions.FindAsync(id)
            ?? throw new KeyNotFoundException($"FuelTransaction {id} not found");

        if (dto.FuelLookupId.HasValue) movement.FuelLookupId = dto.FuelLookupId.Value;
        if (dto.TransactionTypeLookupId.HasValue)
        {
            await ValidateTransactionTypeAsync(dto.TransactionTypeLookupId.Value);
            movement.TransactionTypeLookupId = dto.TransactionTypeLookupId.Value;
        }
        if (dto.TransactionDate.HasValue) movement.TransactionDate = dto.TransactionDate.Value;
        if (dto.QuantityLiters.HasValue) movement.QuantityLiters = dto.QuantityLiters.Value;
        if (dto.Reference is not null) movement.Reference = dto.Reference;
        if (dto.SupplierCompany is not null) movement.SupplierCompany = dto.SupplierCompany;
        if (dto.DriverName is not null) movement.DriverName = dto.DriverName;
        if (dto.DriverIdCard is not null) movement.DriverIdCard = dto.DriverIdCard;
        if (dto.TruckNumber is not null) movement.TruckNumber = dto.TruckNumber;
        if (dto.InvoiceNumber is not null) movement.InvoiceNumber = dto.InvoiceNumber;
        if (dto.InvoiceDate.HasValue) movement.InvoiceDate = dto.InvoiceDate.Value;
        if (dto.InvoiceQuantity.HasValue) movement.InvoiceQuantity = dto.InvoiceQuantity.Value;
        if (dto.MeterId.HasValue) movement.MeterId = dto.MeterId;
        if (dto.PrevMeterVisualReading.HasValue) movement.PrevMeterVisualReading = dto.PrevMeterVisualReading;
        if (dto.CurrentMeterVisualReading.HasValue) movement.CurrentMeterVisualReading = dto.CurrentMeterVisualReading;
        if (dto.PrevMeterStandardReading.HasValue) movement.PrevMeterStandardReading = dto.PrevMeterStandardReading;
        if (dto.CurrentMeterStandardReading.HasValue) movement.CurrentMeterStandardReading = dto.CurrentMeterStandardReading;
        if (dto.SourceSubStationId.HasValue) movement.SourceSubStationId = dto.SourceSubStationId;
        if (dto.DestinationSubStationId.HasValue) movement.DestinationSubStationId = dto.DestinationSubStationId;
        if (dto.CorrectedFromId.HasValue) movement.CorrectedFromId = dto.CorrectedFromId;
        if (dto.CorrectionReason is not null) movement.CorrectionReason = dto.CorrectionReason;
        if (dto.Notes is not null) movement.Notes = dto.Notes;

        // R4+R5: Validate tank capacity for inbound transactions
        var effectiveQuantity = dto.QuantityLiters ?? movement.QuantityLiters;
        var effectiveFuelLookupId = dto.FuelLookupId ?? movement.FuelLookupId;
        var effectiveTransactionTypeLookupId = dto.TransactionTypeLookupId ?? movement.TransactionTypeLookupId;
        var transactionTypeCode = await GetTransactionTypeCodeAsync(effectiveTransactionTypeLookupId);
        if (IsInboundTransaction(transactionTypeCode))
        {
            // Calculate delta for the quantity change
            var quantityDelta = effectiveQuantity - movement.QuantityLiters;
            if (quantityDelta > 0)
            {
                await ValidateInboundCapacityAsync(movement.SubStationId, quantityDelta, effectiveFuelLookupId);
            }
        }

        movement.UpdatedBy = userId;
        movement.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id)
            ?? throw new InvalidOperationException("Failed to retrieve updated transaction.");
    }

    public async Task<FuelMovementDetailDto?> GetByIdAsync(int id)
    {
        return await _db.FuelTransactions
            .AsNoTracking()
            .Include(m => m.FuelLookup)
            .Include(m => m.TransactionTypeLookup)
            .Include(m => m.SubStation)
            .Include(m => m.SourceSubStation)
            .Include(m => m.DestinationSubStation)
            .Include(m => m.Meter)
            .Include(m => m.ApprovedBy)
            .Where(m => m.Id == id && m.DeletedAt == null)
            .Select(m => new FuelMovementDetailDto(
                m.Id,
                m.SubStationId,
                m.SubStation != null ? m.SubStation.Name : "—",
                m.FuelLookupId,
                m.FuelLookup != null ? m.FuelLookup.Name : "—",
                m.TransactionTypeLookupId,
                m.TransactionTypeLookup != null ? m.TransactionTypeLookup.Name : null,
                m.TransactionDate,
                m.QuantityLiters,
                m.Reference,
                m.SupplierCompany,
                m.DriverName,
                m.DriverIdCard,
                m.TruckNumber,
                m.InvoiceNumber,
                m.InvoiceDate,
                m.InvoiceQuantity,
                m.MeterId,
                m.Meter != null ? m.Meter.Name : null,
                m.PrevMeterVisualReading,
                m.CurrentMeterVisualReading,
                m.PrevMeterStandardReading,
                m.CurrentMeterStandardReading,
                m.SourceSubStationId,
                m.SourceSubStation != null ? m.SourceSubStation.Name : null,
                m.DestinationSubStationId,
                m.DestinationSubStation != null ? m.DestinationSubStation.Name : null,
                m.CorrectedFromId,
                m.CorrectionReason,
                m.IsApproved,
                m.ApprovedById,
                m.ApprovedBy != null ? m.ApprovedBy.FullName : null,
                m.ApprovedAt,
                m.CreatedBy,
                m.UpdatedBy,
                m.CreatedAt,
                m.UpdatedAt,
                m.Notes))
            .FirstOrDefaultAsync();
    }

    public async Task<List<FuelMovementListDto>> GetAllAsync()
    {
        var query = _db.FuelTransactions
            .AsNoTracking()
            .Include(m => m.FuelLookup)
            .Include(m => m.TransactionTypeLookup)
            .Include(m => m.SubStation)
            .Include(m => m.SourceSubStation)
            .Include(m => m.DestinationSubStation)
            .Include(m => m.ApprovedBy)
            .Where(m => m.DeletedAt == null)
            .AsQueryable();

        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                if (accessibleIds.Count == 0)
                    return new List<FuelMovementListDto>();
                query = query.Where(m => accessibleIds.Contains(m.SubStationId) ||
                    (m.DestinationSubStationId.HasValue && accessibleIds.Contains(m.DestinationSubStationId.Value)));
            }
        }

        return await query
            .OrderByDescending(m => m.TransactionDate)
            .Select(m => new FuelMovementListDto(
                m.Id,
                m.SubStationId,
                m.SubStation != null ? m.SubStation.Name : "—",
                m.FuelLookupId,
                m.FuelLookup != null ? m.FuelLookup.Name : "—",
                m.TransactionTypeLookupId,
                m.TransactionTypeLookup != null ? m.TransactionTypeLookup.Name : null,
                m.TransactionDate,
                m.QuantityLiters,
                m.Reference,
                m.IsApproved,
                m.ApprovedBy != null ? m.ApprovedBy.FullName : null,
                m.ApprovedAt,
                m.CreatedAt,
                m.SourceSubStation != null ? m.SourceSubStation.Name : null,
                m.DestinationSubStation != null ? m.DestinationSubStation.Name : null,
                m.InvoiceQuantity,
                m.InvoiceNumber,
                m.CorrectedFromId,
                m.CorrectionReason))
            .ToListAsync();
    }

    public async Task<PagedResult<FuelMovementListDto>> GetPagedAsync(int page, int pageSize, string? search = null, string? status = null, string? type = null)
    {
        var query = _db.FuelTransactions
            .AsNoTracking()
            .Include(m => m.FuelLookup)
            .Include(m => m.TransactionTypeLookup)
            .Include(m => m.SubStation)
            .Include(m => m.SourceSubStation)
            .Include(m => m.DestinationSubStation)
            .Include(m => m.ApprovedBy)
            .Where(m => m.DeletedAt == null);

        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                if (accessibleIds.Count == 0)
                    return new PagedResult<FuelMovementListDto> { Items = new List<FuelMovementListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
                query = query.Where(m => accessibleIds.Contains(m.SubStationId) ||
                    (m.DestinationSubStationId.HasValue && accessibleIds.Contains(m.DestinationSubStationId.Value)));
            }
        }

        ApplyFilters(ref query, search, status);
        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalizedType = type.Trim();
            query = query.Where(m => m.TransactionTypeLookup.Code == normalizedType || m.TransactionTypeLookup.Name == normalizedType);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new FuelMovementListDto(
                m.Id,
                m.SubStationId,
                m.SubStation != null ? m.SubStation.Name : "—",
                m.FuelLookupId,
                m.FuelLookup != null ? m.FuelLookup.Name : "—",
                m.TransactionTypeLookupId,
                m.TransactionTypeLookup != null ? m.TransactionTypeLookup.Name : null,
                m.TransactionDate,
                m.QuantityLiters,
                m.Reference,
                m.IsApproved,
                m.ApprovedBy != null ? m.ApprovedBy.FullName : null,
                m.ApprovedAt,
                m.CreatedAt,
                m.SourceSubStation != null ? m.SourceSubStation.Name : null,
                m.DestinationSubStation != null ? m.DestinationSubStation.Name : null,
                m.InvoiceQuantity,
                m.InvoiceNumber,
                m.CorrectedFromId,
                m.CorrectionReason))
            .ToListAsync();

        return new PagedResult<FuelMovementListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<FuelMovementListDto>> GetBySubStationAsync(int subStationId)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                if (!accessibleIds.Contains(subStationId))
                    return new List<FuelMovementListDto>();
            }
        }

        return await _db.FuelTransactions
            .AsNoTracking()
            .Include(m => m.FuelLookup)
            .Include(m => m.TransactionTypeLookup)
            .Include(m => m.SubStation)
            .Include(m => m.SourceSubStation)
            .Include(m => m.DestinationSubStation)
            .Include(m => m.ApprovedBy)
            .Where(m => m.DeletedAt == null && m.SubStationId == subStationId)
            .OrderByDescending(m => m.TransactionDate)
            .Select(m => new FuelMovementListDto(
                m.Id,
                m.SubStationId,
                m.SubStation != null ? m.SubStation.Name : "—",
                m.FuelLookupId,
                m.FuelLookup != null ? m.FuelLookup.Name : "—",
                m.TransactionTypeLookupId,
                m.TransactionTypeLookup != null ? m.TransactionTypeLookup.Name : null,
                m.TransactionDate,
                m.QuantityLiters,
                m.Reference,
                m.IsApproved,
                m.ApprovedBy != null ? m.ApprovedBy.FullName : null,
                m.ApprovedAt,
                m.CreatedAt,
                m.SourceSubStation != null ? m.SourceSubStation.Name : null,
                m.DestinationSubStation != null ? m.DestinationSubStation.Name : null,
                m.InvoiceQuantity,
                m.InvoiceNumber,
                m.CorrectedFromId,
                m.CorrectionReason))
            .ToListAsync();
    }

    public async Task<PagedResult<FuelMovementListDto>> GetPagedBySubStationAsync(int subStationId, int page, int pageSize, string? search = null, string? status = null)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                if (!accessibleIds.Contains(subStationId))
                    return new PagedResult<FuelMovementListDto> { Items = Array.Empty<FuelMovementListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
            }
        }

        var query = _db.FuelTransactions
            .AsNoTracking()
            .Include(m => m.FuelLookup)
            .Include(m => m.TransactionTypeLookup)
            .Include(m => m.SubStation)
            .Include(m => m.SourceSubStation)
            .Include(m => m.DestinationSubStation)
            .Include(m => m.ApprovedBy)
            .Where(m => m.DeletedAt == null && m.SubStationId == subStationId);

        ApplyFilters(ref query, search, status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.TransactionDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new FuelMovementListDto(
                m.Id,
                m.SubStationId,
                m.SubStation != null ? m.SubStation.Name : "—",
                m.FuelLookupId,
                m.FuelLookup != null ? m.FuelLookup.Name : "—",
                m.TransactionTypeLookupId,
                m.TransactionTypeLookup != null ? m.TransactionTypeLookup.Name : null,
                m.TransactionDate,
                m.QuantityLiters,
                m.Reference,
                m.IsApproved,
                m.ApprovedBy != null ? m.ApprovedBy.FullName : null,
                m.ApprovedAt,
                m.CreatedAt,
                m.SourceSubStation != null ? m.SourceSubStation.Name : null,
                m.DestinationSubStation != null ? m.DestinationSubStation.Name : null,
                m.InvoiceQuantity,
                m.InvoiceNumber,
                m.CorrectedFromId,
                m.CorrectionReason))
            .ToListAsync();

        return new PagedResult<FuelMovementListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<FuelMovementListDto>> GetByFuelTankAsync(int fuelTankId)
    {
        var subStationId = await _db.FuelTanks
            .Where(ft => ft.Id == fuelTankId && ft.DeletedAt == null)
            .Select(ft => ft.SubStationId)
            .FirstOrDefaultAsync();

        if (subStationId == 0)
            return new List<FuelMovementListDto>();

        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                if (!accessibleIds.Contains(subStationId))
                    return new List<FuelMovementListDto>();
            }
        }

        return await _db.FuelTransactions
            .AsNoTracking()
            .Include(m => m.FuelLookup)
            .Include(m => m.TransactionTypeLookup)
            .Include(m => m.SubStation)
            .Include(m => m.SourceSubStation)
            .Include(m => m.DestinationSubStation)
            .Include(m => m.ApprovedBy)
            .Where(m => m.DeletedAt == null &&
                (m.SourceSubStationId == subStationId ||
                 m.DestinationSubStationId == subStationId ||
                 m.SubStationId == subStationId))
            .OrderByDescending(m => m.TransactionDate)
            .Select(m => new FuelMovementListDto(
                m.Id,
                m.SubStationId,
                m.SubStation != null ? m.SubStation.Name : "—",
                m.FuelLookupId,
                m.FuelLookup != null ? m.FuelLookup.Name : "—",
                m.TransactionTypeLookupId,
                m.TransactionTypeLookup != null ? m.TransactionTypeLookup.Name : null,
                m.TransactionDate,
                m.QuantityLiters,
                m.Reference,
                m.IsApproved,
                m.ApprovedBy != null ? m.ApprovedBy.FullName : null,
                m.ApprovedAt,
                m.CreatedAt,
                m.SourceSubStation != null ? m.SourceSubStation.Name : null,
                m.DestinationSubStation != null ? m.DestinationSubStation.Name : null,
                m.InvoiceQuantity,
                m.InvoiceNumber,
                m.CorrectedFromId,
                m.CorrectionReason))
            .ToListAsync();
    }

    public async Task<PagedResult<FuelMovementListDto>> GetPagedByFuelTankAsync(int fuelTankId, int page, int pageSize, string? search = null, string? status = null)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (userId.HasValue)
            {
                var tankSubStationId = await _db.FuelTanks
                    .Where(ft => ft.Id == fuelTankId && ft.DeletedAt == null)
                    .Select(ft => ft.SubStationId)
                    .FirstOrDefaultAsync();
                if (tankSubStationId > 0)
                {
                    var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                    if (!accessibleIds.Contains(tankSubStationId))
                        return new PagedResult<FuelMovementListDto> { Items = Array.Empty<FuelMovementListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
                }
            }
        }

        var query = _db.FuelTransactions
            .AsNoTracking()
            .Include(m => m.FuelLookup)
            .Include(m => m.TransactionTypeLookup)
            .Include(m => m.SubStation)
            .Include(m => m.SourceSubStation)
            .Include(m => m.DestinationSubStation)
            .Include(m => m.ApprovedBy)
            .Where(m => m.DeletedAt == null && m.MeterId == fuelTankId);

        ApplyFilters(ref query, search, status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.TransactionDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new FuelMovementListDto(
                m.Id,
                m.SubStationId,
                m.SubStation != null ? m.SubStation.Name : "—",
                m.FuelLookupId,
                m.FuelLookup != null ? m.FuelLookup.Name : "—",
                m.TransactionTypeLookupId,
                m.TransactionTypeLookup != null ? m.TransactionTypeLookup.Name : null,
                m.TransactionDate,
                m.QuantityLiters,
                m.Reference,
                m.IsApproved,
                m.ApprovedBy != null ? m.ApprovedBy.FullName : null,
                m.ApprovedAt,
                m.CreatedAt,
                m.SourceSubStation != null ? m.SourceSubStation.Name : null,
                m.DestinationSubStation != null ? m.DestinationSubStation.Name : null,
                m.InvoiceQuantity,
                m.InvoiceNumber,
                m.CorrectedFromId,
                m.CorrectionReason))
            .ToListAsync();

        return new PagedResult<FuelMovementListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<FuelMovementDetailDto> SubmitForApprovalAsync(int id, int userId)
    {
        var movement = await _db.FuelTransactions.FindAsync(id)
            ?? throw new KeyNotFoundException($"FuelTransaction {id} not found");
        movement.UpdatedBy = userId;
        movement.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(id)
            ?? throw new InvalidOperationException("Failed to retrieve submitted transaction.");
    }

    public async Task<FuelMovementDetailDto> ApproveMovementAsync(int id, int authorizedBy)
    {
        var movement = await _db.FuelTransactions.FindAsync(id)
            ?? throw new KeyNotFoundException($"FuelTransaction {id} not found");

        movement.IsApproved = true;
        movement.ApprovedById = authorizedBy;
        movement.ApprovedAt = DateTime.UtcNow;
        movement.UpdatedAt = DateTime.UtcNow;
        movement.UpdatedBy = authorizedBy;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id)
            ?? throw new InvalidOperationException("Failed to retrieve approved transaction.");
    }

    public async Task<FuelMovementDetailDto> RejectMovementAsync(int id, int authorizedBy, string? rejectionReason = null)
    {
        var movement = await _db.FuelTransactions.FindAsync(id)
            ?? throw new KeyNotFoundException($"FuelTransaction {id} not found");

        movement.IsApproved = false;
        movement.ApprovedById = authorizedBy;
        movement.ApprovedAt = DateTime.UtcNow;
        movement.Notes = string.IsNullOrEmpty(rejectionReason) ? movement.Notes : $"{movement.Notes}\nRejection Reason: {rejectionReason}".Trim();
        movement.UpdatedAt = DateTime.UtcNow;
        movement.UpdatedBy = authorizedBy;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id)
            ?? throw new InvalidOperationException("Failed to retrieve rejected transaction.");
    }

    public async Task DeleteAsync(int id)
    {
        var movement = await _db.FuelTransactions
            .FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt == null)
            ?? throw new KeyNotFoundException($"FuelMovement {id} not found");

        if (movement.IsApproved)
            throw new InvalidOperationException("لا يمكن حذف حركة معتمدة");

        movement.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<FuelMovementDetailDto?> GetLastBySubStationAsync(int subStationId)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                if (!accessibleIds.Contains(subStationId))
                    return null;
            }
        }

        return await _db.FuelTransactions
            .AsNoTracking()
            .Include(m => m.FuelLookup)
            .Include(m => m.TransactionTypeLookup)
            .Include(m => m.SubStation)
            .Include(m => m.SourceSubStation)
            .Include(m => m.DestinationSubStation)
            .Include(m => m.Meter)
            .Include(m => m.ApprovedBy)
            .Where(m => m.DeletedAt == null && m.SubStationId == subStationId)
            .OrderByDescending(m => m.TransactionDate)
            .Select(m => new FuelMovementDetailDto(
                m.Id,
                m.SubStationId,
                m.SubStation != null ? m.SubStation.Name : "—",
                m.FuelLookupId,
                m.FuelLookup != null ? m.FuelLookup.Name : "—",
                m.TransactionTypeLookupId,
                m.TransactionTypeLookup != null ? m.TransactionTypeLookup.Name : null,
                m.TransactionDate,
                m.QuantityLiters,
                m.Reference,
                m.SupplierCompany,
                m.DriverName,
                m.DriverIdCard,
                m.TruckNumber,
                m.InvoiceNumber,
                m.InvoiceDate,
                m.InvoiceQuantity,
                m.MeterId,
                m.Meter != null ? m.Meter.Name : null,
                m.PrevMeterVisualReading,
                m.CurrentMeterVisualReading,
                m.PrevMeterStandardReading,
                m.CurrentMeterStandardReading,
                m.SourceSubStationId,
                m.SourceSubStation != null ? m.SourceSubStation.Name : null,
                m.DestinationSubStationId,
                m.DestinationSubStation != null ? m.DestinationSubStation.Name : null,
                m.CorrectedFromId,
                m.CorrectionReason,
                m.IsApproved,
                m.ApprovedById,
                m.ApprovedBy != null ? m.ApprovedBy.FullName : null,
                m.ApprovedAt,
                m.CreatedBy,
                m.UpdatedBy,
                m.CreatedAt,
                m.UpdatedAt,
                m.Notes))
            .FirstOrDefaultAsync();
    }

    private static void ApplyFilters(ref IQueryable<FuelTransaction> query, string? search, string? status)
    {
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Reference != null && m.Reference.Contains(search));

        if (!string.IsNullOrWhiteSpace(status))
        {
            bool approved = status.Equals("Approved", StringComparison.OrdinalIgnoreCase);
            query = query.Where(m => m.IsApproved == approved);
        }
    }

    /// <summary>
    /// Allowed transaction type codes for fuel movements.
    /// </summary>
    private static readonly HashSet<string> AllowedTransactionTypeCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Purchase", "Transfer", "Loss", "Consumption"
    };

    /// <summary>
    /// Validates that the transaction type is one of the allowed codes.
    /// </summary>
    private async Task ValidateTransactionTypeAsync(int? transactionTypeLookupId)
    {
        if (!transactionTypeLookupId.HasValue)
            throw new InvalidOperationException("Transaction type is required.");

        var code = await GetTransactionTypeCodeAsync(transactionTypeLookupId.Value);
        if (code is null || !AllowedTransactionTypeCodes.Contains(code))
        {
            var allowed = string.Join(", ", AllowedTransactionTypeCodes);
            throw new InvalidOperationException(
                $"Invalid transaction type ID {transactionTypeLookupId.Value}. Allowed types: {allowed}.");
        }
    }

    /// <summary>
    /// Get the lookup Code for a transaction type.
    /// </summary>
    private async Task<string?> GetTransactionTypeCodeAsync(int? transactionTypeLookupId)
    {
        if (!transactionTypeLookupId.HasValue)
            return null;

        return await _db.Lookups
            .Where(l => l.Id == transactionTypeLookupId.Value)
            .Select(l => l.Code)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Determines if a transaction type is inbound (adds fuel to stock).
    /// </summary>
    private static bool IsInboundTransaction(string? code)
    {
        return code == "Purchase" || code == "TransferIn";
    }

    /// <summary>
    /// R4+R5: Validate that inbound fuel quantity won't exceed tank capacity.
    /// </summary>
    private async Task ValidateInboundCapacityAsync(int subStationId, double incomingQuantity, int fuelLookupId)
    {
        // Get all tanks for the substation matching this fuel type
        var tanks = await _db.FuelTanks
            .Where(t => t.SubStationId == subStationId
                     && t.FuelLookupId == fuelLookupId
                     && t.DeletedAt == null)
            .ToListAsync();

        if (tanks.Count == 0)
            return; // No tank configured — skip validation

        var totalCapacity = tanks.Sum(t => t.CapacityLiters);
        var currentStock = tanks.Sum(t => t.ActualStockLiters ?? 0);
        var remaining = totalCapacity - currentStock;

        if (incomingQuantity > remaining)
        {
            var msg = $"R5: كمية الوقود الواردة ({incomingQuantity:N2} لتر) تتجاوز السعة المتبقية ({remaining:N2} لتر) من إجمالي سعة الخزانات ({totalCapacity:N2} لتر)";
            throw new InvalidOperationException(msg);
        }
    }
}
