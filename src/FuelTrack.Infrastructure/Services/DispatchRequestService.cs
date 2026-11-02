using FuelTrack.Application;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class DispatchRequestService : IDispatchRequestService
{
    private readonly FuelTrackDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserScopeFilterService _scopeFilter;

    public DispatchRequestService(FuelTrackDbContext db, ICurrentUserService currentUser, IUserScopeFilterService scopeFilter)
    {
        _db = db;
        _currentUser = currentUser;
        _scopeFilter = scopeFilter;
    }

    public async Task<DispatchRequestDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleSubStationIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleSubStationIds.Count == 0)
                    return null;

                var dispatch = await _db.DispatchRequests
                    .AsNoTracking()
                    .Include(dr => dr.SubStation)
                    .Include(dr => dr.Station)
                    .Include(dr => dr.RequestedBy)
                    .Include(dr => dr.ApprovedBy)
                    .Include(dr => dr.WorkPermit)
                    .Include(dr => dr.StatusLookup)
                    .Include(dr => dr.Items)
                        .ThenInclude(i => i.SparePart)
                            .ThenInclude(sp => sp.UnitLookup)
                    .Where(dr => dr.Id == id && dr.DeletedAt == null && accessibleSubStationIds.Contains(dr.SubStationId))
                    .FirstOrDefaultAsync(ct);

                if (dispatch == null)
                    return null;

                var stocks = await _db.WarehouseStocks
                    .Where(ws => ws.StationId == dispatch.StationId && ws.DeletedAt == null)
                    .ToListAsync(ct);
                var stockLookup = stocks.ToDictionary(ws => ws.SparePartId, ws => ws.AvailableQuantity);

                return MapToDetail(dispatch, stockLookup);
            }
        }

        var dispatch2 = await _db.DispatchRequests
            .AsNoTracking()
            .Include(dr => dr.SubStation)
            .Include(dr => dr.Station)
            .Include(dr => dr.RequestedBy)
            .Include(dr => dr.ApprovedBy)
            .Include(dr => dr.WorkPermit)
            .Include(dr => dr.StatusLookup)
            .Include(dr => dr.Items)
                .ThenInclude(i => i.SparePart)
                    .ThenInclude(sp => sp.UnitLookup)
            .Where(dr => dr.Id == id && dr.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (dispatch2 == null)
            return null;

        var stocks2 = await _db.WarehouseStocks
            .Where(ws => ws.StationId == dispatch2.StationId && ws.DeletedAt == null)
            .ToListAsync(ct);
        var stockLookup2 = stocks2.ToDictionary(ws => ws.SparePartId, ws => ws.AvailableQuantity);

        return MapToDetail(dispatch2, stockLookup2);
    }

    public async Task<PagedResult<DispatchRequestListDto>> GetBySubStationAsync(int subStationId, int page, int pageSize, CancellationToken ct = default)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (!accessibleIds.Contains(subStationId))
                    return new PagedResult<DispatchRequestListDto> { Items = Array.Empty<DispatchRequestListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
            }
        }

        var query = _db.DispatchRequests
            .AsNoTracking()
            .Include(dr => dr.SubStation)
            .Include(dr => dr.Station)
            .Include(dr => dr.StatusLookup)
            .Include(dr => dr.RequestedBy)
            .Include(dr => dr.WorkPermit)
            .Include(dr => dr.Items)
            .Where(dr => dr.SubStationId == subStationId && dr.DeletedAt == null);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(dr => dr.RequestDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(dr => new DispatchRequestListDto(
                dr.Id,
                dr.RequestNumber,
                dr.SubStation.Name,
                dr.Station.Name,
                dr.RequestDate,
                dr.StatusLookup.Name,
                dr.RequestedBy.FullName,
                dr.RequestedById,
                dr.Items.Count,
                dr.WorkPermit != null ? dr.WorkPermit.PermitNumber : null,
                dr.WorkPermitId,
                dr.Purpose,
                dr.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<DispatchRequestListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<DispatchRequestListDto>> GetByStationAsync(int stationId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.DispatchRequests
            .AsNoTracking()
            .Include(dr => dr.SubStation)
            .Include(dr => dr.Station)
            .Include(dr => dr.StatusLookup)
            .Include(dr => dr.RequestedBy)
            .Include(dr => dr.WorkPermit)
            .Include(dr => dr.Items)
            .Where(dr => dr.StationId == stationId && dr.DeletedAt == null)
            .AsQueryable();

        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleSubStationIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleSubStationIds.Count == 0)
                    return new PagedResult<DispatchRequestListDto> { Items = Array.Empty<DispatchRequestListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
                query = query.Where(dr => accessibleSubStationIds.Contains(dr.SubStationId));
            }
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(dr => dr.RequestDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(dr => new DispatchRequestListDto(
                dr.Id,
                dr.RequestNumber,
                dr.SubStation.Name,
                dr.Station.Name,
                dr.RequestDate,
                dr.StatusLookup.Name,
                dr.RequestedBy.FullName,
                dr.RequestedById,
                dr.Items.Count,
                dr.WorkPermit != null ? dr.WorkPermit.PermitNumber : null,
                dr.WorkPermitId,
                dr.Purpose,
                dr.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<DispatchRequestListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<DispatchRequestListDto>> GetAllAsync(int page, int pageSize, int? status = null, CancellationToken ct = default)
    {
        var query = _db.DispatchRequests
            .AsNoTracking()
            .Include(dr => dr.SubStation)
            .Include(dr => dr.Station)
            .Include(dr => dr.StatusLookup)
            .Include(dr => dr.RequestedBy)
            .Include(dr => dr.WorkPermit)
            .Include(dr => dr.Items)
            .Where(dr => dr.DeletedAt == null)
            .AsQueryable();

        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleSubStationIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleSubStationIds.Count == 0)
                    return new PagedResult<DispatchRequestListDto> { Items = Array.Empty<DispatchRequestListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
                query = query.Where(dr => accessibleSubStationIds.Contains(dr.SubStationId));
            }
        }

        if (status.HasValue)
            query = query.Where(dr => dr.StatusLookupId == status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(dr => dr.RequestDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(dr => new DispatchRequestListDto(
                dr.Id,
                dr.RequestNumber,
                dr.SubStation.Name,
                dr.Station.Name,
                dr.RequestDate,
                dr.StatusLookup.Name,
                dr.RequestedBy.FullName,
                dr.RequestedById,
                dr.Items.Count,
                dr.WorkPermit != null ? dr.WorkPermit.PermitNumber : null,
                dr.WorkPermitId,
                dr.Purpose,
                dr.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<DispatchRequestListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Result<DispatchRequestDetailDto>> CreateAsync(CreateDispatchRequestDto dto, int userId, CancellationToken ct = default)
    {
        var subStation = await _db.SubStations.FirstOrDefaultAsync(ss => ss.Id == dto.SubStationId && ss.DeletedAt == null, ct);
        if (subStation is null)
            return Result<DispatchRequestDetailDto>.NotFound("SubStation not found.");

        var station = await _db.Stations.FirstOrDefaultAsync(s => s.Id == dto.StationId && s.DeletedAt == null, ct);
        if (station is null)
            return Result<DispatchRequestDetailDto>.NotFound("Station not found.");

        if (dto.Items == null || dto.Items.Count == 0)
            return Result<DispatchRequestDetailDto>.Failure("Dispatch request must contain at least one item.");

        var requestNumber = await GenerateRequestNumberAsync(dto.SubStationId, ct);
        var pendingStatus = await _db.Lookups
            .FirstOrDefaultAsync(l => l.Category.Name == "DispatchStatus" && l.Code == "Pending", ct);

        var dispatch = new DispatchRequest
        {
            RequestNumber = requestNumber,
            SubStationId = dto.SubStationId,
            StationId = dto.StationId,
            RequestDate = DateTime.UtcNow,
            RequestedById = userId,
            WorkPermitId = dto.WorkPermitId,
            StatusLookupId = pendingStatus != null ? pendingStatus.Id : 0,
            Purpose = dto.Purpose,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _db.DispatchRequests.Add(dispatch);
        await _db.SaveChangesAsync(ct);

        var items = new List<DispatchRequestItem>();
        foreach (var itemDto in dto.Items)
        {
            var sparePart = await _db.SpareParts.FirstOrDefaultAsync(sp => sp.Id == itemDto.SparePartId && sp.DeletedAt == null, ct);
            if (sparePart == null)
                return Result<DispatchRequestDetailDto>.Failure($"Spare part {itemDto.SparePartId} not found.");

            var stock = await _db.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.SparePartId == itemDto.SparePartId && ws.StationId == dto.StationId && ws.DeletedAt == null, ct);

            if (stock == null)
            {
                stock = new WarehouseStock
                {
                    SparePartId = itemDto.SparePartId,
                    StationId = dto.StationId,
                    CurrentQuantity = 0,
                    ReservedQuantity = itemDto.RequestedQuantity,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = userId
                };
                _db.WarehouseStocks.Add(stock);
            }
            else
            {
                stock.ReservedQuantity += itemDto.RequestedQuantity;
                stock.UpdatedAt = DateTime.UtcNow;
                stock.UpdatedBy = userId;
            }

            items.Add(new DispatchRequestItem
            {
                DispatchRequestId = dispatch.Id,
                SparePartId = itemDto.SparePartId,
                RequestedQuantity = itemDto.RequestedQuantity,
                ApprovedQuantity = null,
                IssuedQuantity = null,
                Notes = itemDto.Notes,
                IsApproved = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            });
        }

        _db.DispatchRequestItems.AddRange(items);
        await _db.SaveChangesAsync(ct);

        var detail = await GetByIdAsync(dispatch.Id, ct);
        return Result<DispatchRequestDetailDto>.Success(detail!);
    }

    public async Task<Result<DispatchRequestDetailDto>> UpdateAsync(int id, UpdateDispatchRequestDto dto, int userId, CancellationToken ct = default)
    {
        var dispatch = await _db.DispatchRequests
            .Include(dr => dr.Items)
            .Include(dr => dr.StatusLookup)
            .FirstOrDefaultAsync(dr => dr.Id == id && dr.DeletedAt == null, ct);

        if (dispatch is null)
            return Result<DispatchRequestDetailDto>.NotFound($"Dispatch request {id} not found.");

        if (dispatch.StatusLookup.Code == "Issued" || dispatch.StatusLookup.Code == "Completed")
            return Result<DispatchRequestDetailDto>.Failure("Cannot update an issued or completed dispatch request.");

        dispatch.WorkPermitId = dto.WorkPermitId;
        dispatch.Purpose = dto.Purpose;
        dispatch.Notes = dto.Notes;
        dispatch.UpdatedAt = DateTime.UtcNow;
        dispatch.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);

        var detail = await GetByIdAsync(dispatch.Id, ct);
        return Result<DispatchRequestDetailDto>.Success(detail!);
    }

    public async Task<Result<DispatchRequestDetailDto>> ApproveAsync(int id, ApproveDispatchRequestDto dto, int userId, CancellationToken ct = default)
    {
        var dispatch = await _db.DispatchRequests
            .Include(dr => dr.Items)
            .Include(dr => dr.StatusLookup)
            .FirstOrDefaultAsync(dr => dr.Id == id && dr.DeletedAt == null, ct);

        if (dispatch is null)
            return Result<DispatchRequestDetailDto>.NotFound($"Dispatch request {id} not found.");

        var pendingStatus = await _db.Lookups
            .FirstOrDefaultAsync(l => l.Category.Name == "DispatchStatus" && l.Code == "Pending", ct);
        if (pendingStatus == null || dispatch.StatusLookupId != pendingStatus.Id)
            return Result<DispatchRequestDetailDto>.Failure("Only pending dispatch requests can be approved.");

        if (dto.Items == null || dto.Items.Count == 0)
            return Result<DispatchRequestDetailDto>.Failure("Approval must contain at least one item.");

        var allItemIds = dispatch.Items.Select(i => i.Id).ToList();
        foreach (var approveItem in dto.Items)
        {
            if (!allItemIds.Contains(approveItem.DispatchRequestItemId))
                return Result<DispatchRequestDetailDto>.Failure($"Item {approveItem.DispatchRequestItemId} does not belong to this request.");

            if (!approveItem.IsApproved)
                continue;

            var item = dispatch.Items.First(i => i.Id == approveItem.DispatchRequestItemId);
            var availableStock = await GetAvailableInStockAsync(item.SparePartId, dispatch.StationId, ct);
            if (approveItem.ApprovedQuantity > availableStock)
                return Result<DispatchRequestDetailDto>.Failure($"Approved quantity ({approveItem.ApprovedQuantity}) exceeds available stock ({availableStock}) for part {item.SparePart.Name}.");

            item.ApprovedQuantity = approveItem.ApprovedQuantity;
            item.IsApproved = true;
        }

        var allApproved = dto.Items.All(i => i.IsApproved);
        var anyApproved = dto.Items.Any(i => i.IsApproved);

        if (allApproved)
        {
            var approvedStatus = await _db.Lookups
                .FirstOrDefaultAsync(l => l.Category.Name == "DispatchStatus" && l.Code == "Approved", ct);
            if (approvedStatus != null)
                dispatch.StatusLookupId = approvedStatus.Id;
        }
        else if (anyApproved)
        {
            var partialStatus = await _db.Lookups
                .FirstOrDefaultAsync(l => l.Category.Name == "DispatchStatus" && l.Code == "PartialApproved", ct);
            if (partialStatus != null)
                dispatch.StatusLookupId = partialStatus.Id;
        }
        else
        {
            var rejectedStatus = await _db.Lookups
                .FirstOrDefaultAsync(l => l.Category.Name == "DispatchStatus" && l.Code == "Rejected", ct);
            if (rejectedStatus != null)
                dispatch.StatusLookupId = rejectedStatus.Id;
        }

        dispatch.UpdatedAt = DateTime.UtcNow;
        dispatch.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);

        var detail = await GetByIdAsync(dispatch.Id, ct);
        return Result<DispatchRequestDetailDto>.Success(detail!);
    }

    public async Task<Result<DispatchRequestDetailDto>> RejectAsync(int id, string reason, int userId, CancellationToken ct = default)
    {
        var dispatch = await _db.DispatchRequests
            .Include(dr => dr.Items)
            .Include(dr => dr.StatusLookup)
            .FirstOrDefaultAsync(dr => dr.Id == id && dr.DeletedAt == null, ct);

        if (dispatch is null)
            return Result<DispatchRequestDetailDto>.NotFound($"Dispatch request {id} not found.");

        var pendingStatus = await _db.Lookups
            .FirstOrDefaultAsync(l => l.Category.Name == "DispatchStatus" && l.Code == "Pending", ct);
        if (pendingStatus == null || (dispatch.StatusLookupId != pendingStatus.Id && dispatch.StatusLookup.Code != "PartialApproved"))
            return Result<DispatchRequestDetailDto>.Failure("Only pending or partially approved dispatch requests can be rejected.");

        var rejectedStatus = await _db.Lookups
            .FirstOrDefaultAsync(l => l.Category.Name == "DispatchStatus" && l.Code == "Rejected", ct);

        dispatch.StatusLookupId = rejectedStatus != null ? rejectedStatus.Id : dispatch.StatusLookupId;
        dispatch.Notes = string.IsNullOrEmpty(reason) ? dispatch.Notes : $"{dispatch.Notes}\nRejection Reason: {reason}".Trim();
        dispatch.UpdatedAt = DateTime.UtcNow;
        dispatch.UpdatedBy = userId;

        foreach (var item in dispatch.Items)
        {
            if (item.ApprovedQuantity.HasValue && item.ApprovedQuantity.Value > 0)
            {
                item.ApprovedQuantity = null;
                item.IsApproved = false;

                var stock = await _db.WarehouseStocks
                    .FirstOrDefaultAsync(ws => ws.SparePartId == item.SparePartId && ws.StationId == dispatch.StationId && ws.DeletedAt == null, ct);
                if (stock != null)
                {
                    stock.ReservedQuantity -= item.RequestedQuantity;
                    if (stock.ReservedQuantity < 0) stock.ReservedQuantity = 0;
                    stock.UpdatedAt = DateTime.UtcNow;
                    stock.UpdatedBy = userId;
                }
            }
        }

        await _db.SaveChangesAsync(ct);

        var detail = await GetByIdAsync(dispatch.Id, ct);
        return Result<DispatchRequestDetailDto>.Success(detail!);
    }

    public async Task<Result<DispatchRequestDetailDto>> IssueAsync(int id, int userId, CancellationToken ct = default)
    {
        var dispatch = await _db.DispatchRequests
            .Include(dr => dr.Items)
                .ThenInclude(i => i.SparePart)
            .Include(dr => dr.StatusLookup)
            .FirstOrDefaultAsync(dr => dr.Id == id && dr.DeletedAt == null, ct);

        if (dispatch is null)
            return Result<DispatchRequestDetailDto>.NotFound($"Dispatch request {id} not found.");

        if (dispatch.StatusLookup.Code != "Approved" && dispatch.StatusLookup.Code != "PartialApproved")
            return Result<DispatchRequestDetailDto>.Failure("Only approved dispatch requests can be issued.");

        var issuedStatus = await _db.Lookups
            .FirstOrDefaultAsync(l => l.Category.Name == "DispatchStatus" && l.Code == "Issued", ct);

        dispatch.StatusLookupId = issuedStatus != null ? issuedStatus.Id : dispatch.StatusLookupId;
        dispatch.UpdatedAt = DateTime.UtcNow;
        dispatch.UpdatedBy = userId;

        foreach (var item in dispatch.Items.Where(i => i.IsApproved && i.ApprovedQuantity.HasValue))
        {
            item.IssuedQuantity = item.ApprovedQuantity;

            var stock = await _db.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.SparePartId == item.SparePartId && ws.StationId == dispatch.StationId && ws.DeletedAt == null, ct);

            if (stock == null)
                return Result<DispatchRequestDetailDto>.Failure($"Stock not found for part {item.SparePart.Name}.");

            var balanceBefore = stock.CurrentQuantity;
            stock.CurrentQuantity -= item.IssuedQuantity.Value;
            stock.ReservedQuantity -= item.RequestedQuantity;
            if (stock.CurrentQuantity < 0) stock.CurrentQuantity = 0;
            if (stock.ReservedQuantity < 0) stock.ReservedQuantity = 0;
            stock.UpdatedAt = DateTime.UtcNow;
            stock.UpdatedBy = userId;

            var transactionTypeLookup = await _db.Lookups
                .FirstOrDefaultAsync(l => l.Category.Name == "WarehouseTransactionType" && l.Code == "Dispatch", ct);

            var transaction = new WarehouseTransaction
            {
                SparePartId = item.SparePartId,
                StationId = dispatch.StationId,
                TransactionTypeLookupId = transactionTypeLookup != null ? transactionTypeLookup.Id : 0,
                TransactionDate = DateTime.UtcNow,
                Quantity = item.IssuedQuantity.Value,
                BalanceBefore = balanceBefore,
                BalanceAfter = stock.CurrentQuantity,
                ReferenceId = dispatch.Id,
                ReferenceType = "DispatchRequest",
                Notes = $"Dispatch {dispatch.RequestNumber}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            };
            _db.WarehouseTransactions.Add(transaction);
        }

        await _db.SaveChangesAsync(ct);

        var detail = await GetByIdAsync(dispatch.Id, ct);
        return Result<DispatchRequestDetailDto>.Success(detail!);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var dispatch = await _db.DispatchRequests
            .Include(dr => dr.Items)
            .Include(dr => dr.StatusLookup)
            .FirstOrDefaultAsync(dr => dr.Id == id && dr.DeletedAt == null, ct);

        if (dispatch is null)
            return Result.NotFound($"Dispatch request {id} not found.");

        if (dispatch.StatusLookup.Code == "Issued" || dispatch.StatusLookup.Code == "Completed")
            return Result.Failure("Cannot delete an issued or completed dispatch request.");

        foreach (var item in dispatch.Items)
        {
            var stock = await _db.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.SparePartId == item.SparePartId && ws.StationId == dispatch.StationId && ws.DeletedAt == null, ct);
            if (stock != null)
            {
                stock.ReservedQuantity -= item.RequestedQuantity;
                if (stock.ReservedQuantity < 0) stock.ReservedQuantity = 0;
                stock.UpdatedAt = DateTime.UtcNow;
                stock.UpdatedBy = 0;
            }
        }

        dispatch.IsActive = false;
        dispatch.DeletedAt = DateTime.UtcNow;
        dispatch.UpdatedAt = DateTime.UtcNow;
        dispatch.UpdatedBy = 0;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<decimal> GetAvailableInStockAsync(int sparePartId, int stationId, CancellationToken ct)
    {
        var stock = await _db.WarehouseStocks
            .FirstOrDefaultAsync(ws => ws.SparePartId == sparePartId && ws.StationId == stationId && ws.DeletedAt == null, ct);

        if (stock == null)
            return 0;

        return stock.CurrentQuantity - stock.ReservedQuantity;
    }

    private async Task<string> GenerateRequestNumberAsync(int subStationId, CancellationToken ct)
    {
        var subStation = await _db.SubStations.FirstOrDefaultAsync(ss => ss.Id == subStationId && ss.DeletedAt == null, ct);
        if (subStation == null)
            throw new InvalidOperationException("SubStation not found.");

        var year = DateTime.UtcNow.Year;
        var prefix = $"DRQ-{year}-{subStation.Code}";

        var lastRequest = await _db.DispatchRequests
            .Where(dr => dr.RequestNumber.StartsWith(prefix) && dr.DeletedAt == null)
            .OrderByDescending(dr => dr.RequestNumber)
            .FirstOrDefaultAsync(ct);

        int seq = 1;
        if (lastRequest != null)
        {
            var parts = lastRequest.RequestNumber.Split('-');
            if (parts.Length == 4 && int.TryParse(parts[3], out var lastSeq))
                seq = lastSeq + 1;
        }

        return $"{prefix}-{seq:D4}";
    }

    public async Task<List<DispatchApprovalItemDto>> GetApprovalDataAsync(int id, CancellationToken ct = default)
    {
        var dispatch = await _db.DispatchRequests
            .AsNoTracking()
            .Include(dr => dr.Items)
                .ThenInclude(i => i.SparePart)
            .FirstOrDefaultAsync(dr => dr.Id == id && dr.DeletedAt == null, ct);

        if (dispatch is null)
            return new List<DispatchApprovalItemDto>();

        var result = new List<DispatchApprovalItemDto>();
        foreach (var item in dispatch.Items)
        {
            var availableStock = await GetAvailableInStockAsync(item.SparePartId, dispatch.StationId, ct);
            result.Add(new DispatchApprovalItemDto(
                item.Id,
                item.SparePart.Name,
                item.RequestedQuantity,
                availableStock));
        }
        return result;
    }

    private static DispatchRequestDetailDto MapToDetail(DispatchRequest dr, Dictionary<int, decimal> stockLookup)
    {
        return new DispatchRequestDetailDto(
            dr.Id,
            dr.RequestNumber,
            dr.SubStationId,
            dr.SubStation.Name,
            dr.StationId,
            dr.Station.Name,
            dr.RequestDate,
            dr.RequestedById,
            dr.RequestedBy.FullName,
            dr.ApprovedById,
            dr.ApprovedBy != null ? dr.ApprovedBy.FullName : null,
            dr.ApprovalDate,
            dr.WorkPermitId,
            dr.WorkPermit != null ? dr.WorkPermit.PermitNumber : null,
            dr.StatusLookupId,
            dr.StatusLookup.Name,
            dr.Purpose,
            dr.Notes,
            dr.CreatedAt,
            dr.Items.Select(i => new DispatchItemDetailDto(
                i.Id,
                i.SparePartId,
                i.SparePart.Name,
                i.SparePart.PartNumber,
                i.SparePart.UnitLookup.Name,
                i.RequestedQuantity,
                i.ApprovedQuantity,
                i.IssuedQuantity,
                stockLookup.GetValueOrDefault(i.SparePartId),
                i.IsApproved,
                i.Notes)).ToList());
    }
}
