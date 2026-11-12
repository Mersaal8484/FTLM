using FuelTrack.Application;
using FuelTrack.Application.DTOs.Movements;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class MovementLedgerService(
    FuelTrackDbContext db,
    IUserScopeFilterService scopeFilter,
    ICurrentUserService currentUser) : IMovementLedgerService
{
    public async Task<PagedResult<MovementLedgerRowDto>> GetAsync(
        MovementLedgerFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);
        List<int>? accessibleIds = null;

        if (!currentUser.HasAnyAdminRole)
        {
            accessibleIds = currentUser.UserId.HasValue
                ? await scopeFilter.GetAccessibleSubStationIdsAsync(currentUser.UserId.Value)
                : [];
        }

        var fuel = db.FuelTransactions.AsNoTracking().Where(x => x.DeletedAt == null);
        var oil = db.OilTransactions.AsNoTracking().Where(x => x.DeletedAt == null);

        if (accessibleIds is not null)
        {
            fuel = fuel.Where(x => accessibleIds.Contains(x.SubStationId));
            oil = oil.Where(x => accessibleIds.Contains(x.SubStationId));
        }

        if (filter.DateFrom.HasValue)
        {
            fuel = fuel.Where(x => x.TransactionDate >= filter.DateFrom.Value);
            oil = oil.Where(x => x.TransactionDate >= filter.DateFrom.Value);
        }
        if (filter.DateTo.HasValue)
        {
            var exclusiveEnd = filter.DateTo.Value.Date.AddDays(1);
            fuel = fuel.Where(x => x.TransactionDate < exclusiveEnd);
            oil = oil.Where(x => x.TransactionDate < exclusiveEnd);
        }
        if (filter.StationId.HasValue)
        {
            fuel = fuel.Where(x => x.SubStation.StationId == filter.StationId.Value);
            oil = oil.Where(x => x.SubStation.StationId == filter.StationId.Value);
        }
        if (filter.SubStationId.HasValue)
        {
            fuel = fuel.Where(x => x.SubStationId == filter.SubStationId.Value);
            oil = oil.Where(x => x.SubStationId == filter.SubStationId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.MovementType))
        {
            var type = filter.MovementType.Trim();
            fuel = fuel.Where(x => x.TransactionTypeLookup.Code == type || x.TransactionTypeLookup.Name == type);
            oil = oil.Where(x => x.TransactionTypeLookup.Code == type || x.TransactionTypeLookup.Name == type);
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var status = filter.Status.Trim().ToLowerInvariant();
            fuel = status switch
            {
                "approved" => fuel.Where(x => x.IsApproved),
                "rejected" => fuel.Where(x => !x.IsApproved && x.ApprovedAt != null),
                _ => fuel.Where(x => !x.IsApproved && x.ApprovedAt == null)
            };
            oil = status switch
            {
                "approved" => oil.Where(x => x.IsApproved),
                "rejected" => oil.Where(x => !x.IsApproved && x.ApprovedAt != null),
                _ => oil.Where(x => !x.IsApproved && x.ApprovedAt == null)
            };
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            fuel = fuel.Where(x =>
                (x.Reference != null && x.Reference.Contains(search)) ||
                (x.SupplierCompany != null && x.SupplierCompany.Contains(search)) ||
                x.SubStation.Name.Contains(search) ||
                x.TransactionTypeLookup.Name.Contains(search));
            oil = oil.Where(x =>
                (x.Reference != null && x.Reference.Contains(search)) ||
                (x.SupplierCompany != null && x.SupplierCompany.Contains(search)) ||
                x.SubStation.Name.Contains(search) ||
                x.TransactionTypeLookup.Name.Contains(search));
        }

        IQueryable<MovementLedgerRowDto> query;
        if (string.Equals(filter.Kind, "fuel", StringComparison.OrdinalIgnoreCase))
            query = ProjectFuel(fuel);
        else if (string.Equals(filter.Kind, "oil", StringComparison.OrdinalIgnoreCase))
            query = ProjectOil(oil);
        else
            query = ProjectFuel(fuel).Concat(ProjectOil(oil));

        var descending = !string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = (filter.Sort ?? "date").ToLowerInvariant() switch
        {
            "quantity" => descending ? query.OrderByDescending(x => x.Quantity) : query.OrderBy(x => x.Quantity),
            "station" => descending ? query.OrderByDescending(x => x.Station) : query.OrderBy(x => x.Station),
            "type" => descending ? query.OrderByDescending(x => x.MovementType) : query.OrderBy(x => x.MovementType),
            _ => descending ? query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id) : query.OrderBy(x => x.Date).ThenBy(x => x.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<MovementLedgerRowDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private IQueryable<MovementLedgerRowDto> ProjectFuel(IQueryable<FuelTransaction> query) =>
        query.Select(x => new MovementLedgerRowDto(
            x.Id, "fuel", x.TransactionDate,
            x.SubStation.StationId, x.SubStation.Station.Name,
            x.SubStationId, x.SubStation.Name,
            x.TransactionTypeLookup.Name, x.TransactionTypeLookup.Code,
            x.QuantityLiters, "L",
            x.SupplierCompany ?? x.Reference ?? (x.DestinationSubStation != null ? x.DestinationSubStation.Name : null),
            x.IsApproved ? "approved" : x.ApprovedAt != null ? "rejected" : "pending",
            db.Users.Where(u => u.Id == x.CreatedBy).Select(u => u.FullName).FirstOrDefault(),
            x.ApprovedBy != null ? x.ApprovedBy.FullName : null,
            x.CreatedAt));

    private IQueryable<MovementLedgerRowDto> ProjectOil(IQueryable<OilTransaction> query) =>
        query.Select(x => new MovementLedgerRowDto(
            x.Id, "oil", x.TransactionDate,
            x.SubStation.StationId, x.SubStation.Station.Name,
            x.SubStationId, x.SubStation.Name,
            x.TransactionTypeLookup.Name, x.TransactionTypeLookup.Code,
            x.QuantityLiters, "L",
            x.SupplierCompany ?? x.Reference ?? (x.DestinationSubStation != null ? x.DestinationSubStation.Name : null),
            x.IsApproved ? "approved" : x.ApprovedAt != null ? "rejected" : "pending",
            db.Users.Where(u => u.Id == x.CreatedBy).Select(u => u.FullName).FirstOrDefault(),
            x.ApprovedBy != null ? x.ApprovedBy.FullName : null,
            x.CreatedAt));
}
