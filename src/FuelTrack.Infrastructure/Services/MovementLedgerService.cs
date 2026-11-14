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
    private sealed class LedgerKeyRow
    {
        public int Id { get; set; }
        public string Kind { get; set; } = null!;
        public DateTime Date { get; set; }
        public string Station { get; set; } = null!;
        public string MovementType { get; set; } = null!;
        public double Quantity { get; set; }
    }

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

        IQueryable<LedgerKeyRow> keyQuery;
        if (string.Equals(filter.Kind, "fuel", StringComparison.OrdinalIgnoreCase))
            keyQuery = ProjectFuelKey(fuel);
        else if (string.Equals(filter.Kind, "oil", StringComparison.OrdinalIgnoreCase))
            keyQuery = ProjectOilKey(oil);
        else
            keyQuery = ProjectFuelKey(fuel).Concat(ProjectOilKey(oil));

        var descending = !string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        keyQuery = (filter.Sort ?? "date").ToLowerInvariant() switch
        {
            "quantity" => descending ? keyQuery.OrderByDescending(x => x.Quantity) : keyQuery.OrderBy(x => x.Quantity),
            "station" => descending ? keyQuery.OrderByDescending(x => x.Station) : keyQuery.OrderBy(x => x.Station),
            "type" => descending ? keyQuery.OrderByDescending(x => x.MovementType) : keyQuery.OrderBy(x => x.MovementType),
            _ => descending ? keyQuery.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id) : keyQuery.OrderBy(x => x.Date).ThenBy(x => x.Id)
        };

        var totalCount = await keyQuery.CountAsync(cancellationToken);
        var pageKeys = await keyQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        if (pageKeys.Count == 0)
        {
            return new PagedResult<MovementLedgerRowDto>
            {
                Items = [],
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        var fuelIds = pageKeys.Where(x => x.Kind == "fuel").Select(x => x.Id).ToList();
        var oilIds = pageKeys.Where(x => x.Kind == "oil").Select(x => x.Id).ToList();

        var fuelEntities = fuelIds.Count > 0
            ? await db.FuelTransactions.AsNoTracking()
                .Include(x => x.SubStation).ThenInclude(s => s.Station)
                .Include(x => x.TransactionTypeLookup)
                .Include(x => x.DestinationSubStation)
                .Include(x => x.ApprovedBy)
                .Where(x => fuelIds.Contains(x.Id))
                .ToListAsync(cancellationToken)
            : [];

        var oilEntities = oilIds.Count > 0
            ? await db.OilTransactions.AsNoTracking()
                .Include(x => x.SubStation).ThenInclude(s => s.Station)
                .Include(x => x.TransactionTypeLookup)
                .Include(x => x.DestinationSubStation)
                .Include(x => x.ApprovedBy)
                .Where(x => oilIds.Contains(x.Id))
                .ToListAsync(cancellationToken)
            : [];

        var userIds = fuelEntities.Select(x => x.CreatedBy)
            .Concat(oilEntities.Select(x => x.CreatedBy))
            .Concat(fuelEntities.Where(x => x.ApprovedById.HasValue).Select(x => x.ApprovedById!.Value))
            .Concat(oilEntities.Where(x => x.ApprovedById.HasValue).Select(x => x.ApprovedById!.Value))
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var userNames = userIds.Count > 0
            ? await db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken)
            : new Dictionary<int, string>();

        var fuelMap = fuelEntities.ToDictionary(
            x => x.Id,
            x => new MovementLedgerRowDto(
                x.Id,
                "fuel",
                x.TransactionDate,
                x.SubStation.StationId,
                x.SubStation.Station.Name,
                x.SubStationId,
                x.SubStation.Name,
                x.TransactionTypeLookup.Name,
                x.TransactionTypeLookup.Code,
                x.QuantityLiters,
                "L",
                x.SupplierCompany ?? x.Reference ?? x.DestinationSubStation?.Name,
                x.IsApproved ? "approved" : x.ApprovedAt != null ? "rejected" : "pending",
                userNames.TryGetValue(x.CreatedBy, out var cb) ? cb : null,
                x.ApprovedBy?.FullName ?? (x.ApprovedById.HasValue && userNames.TryGetValue(x.ApprovedById.Value, out var ab) ? ab : null),
                x.CreatedAt));

        var oilMap = oilEntities.ToDictionary(
            x => x.Id,
            x => new MovementLedgerRowDto(
                x.Id,
                "oil",
                x.TransactionDate,
                x.SubStation.StationId,
                x.SubStation.Station.Name,
                x.SubStationId,
                x.SubStation.Name,
                x.TransactionTypeLookup.Name,
                x.TransactionTypeLookup.Code,
                x.QuantityLiters,
                "L",
                x.SupplierCompany ?? x.Reference ?? x.DestinationSubStation?.Name,
                x.IsApproved ? "approved" : x.ApprovedAt != null ? "rejected" : "pending",
                userNames.TryGetValue(x.CreatedBy, out var cb) ? cb : null,
                x.ApprovedBy?.FullName ?? (x.ApprovedById.HasValue && userNames.TryGetValue(x.ApprovedById.Value, out var ab) ? ab : null),
                x.CreatedAt));

        var items = new List<MovementLedgerRowDto>(pageKeys.Count);
        foreach (var key in pageKeys)
        {
            if (key.Kind == "fuel" && fuelMap.TryGetValue(key.Id, out var fuelDto))
            {
                items.Add(fuelDto);
            }
            else if (key.Kind == "oil" && oilMap.TryGetValue(key.Id, out var oilDto))
            {
                items.Add(oilDto);
            }
        }

        return new PagedResult<MovementLedgerRowDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static IQueryable<LedgerKeyRow> ProjectFuelKey(IQueryable<FuelTransaction> query) =>
        query.Select(x => new LedgerKeyRow
        {
            Id = x.Id,
            Kind = "fuel",
            Date = x.TransactionDate,
            Station = x.SubStation.Station.Name,
            MovementType = x.TransactionTypeLookup.Name,
            Quantity = x.QuantityLiters
        });

    private static IQueryable<LedgerKeyRow> ProjectOilKey(IQueryable<OilTransaction> query) =>
        query.Select(x => new LedgerKeyRow
        {
            Id = x.Id,
            Kind = "oil",
            Date = x.TransactionDate,
            Station = x.SubStation.Station.Name,
            MovementType = x.TransactionTypeLookup.Name,
            Quantity = x.QuantityLiters
        });
}
