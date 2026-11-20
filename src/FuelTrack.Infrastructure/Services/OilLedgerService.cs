using FuelTrack.Application.DTOs.OilLedger;
using FuelTrack.Application.Interfaces;
using FuelTrack.Application.Services;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

/// <summary>Read-only monthly oil ledger. Balances are derived, never persisted.</summary>
public sealed class OilLedgerService(
    FuelTrackDbContext db,
    IUserScopeFilterService scopeFilter,
    ICurrentUserService currentUser,
    ICommercialStationModeService commercialMode) : IOilLedgerService
{
    private sealed record TransactionRow(
        int Id, int SubStationId, int? DestinationSubStationId, int OilLookupId,
        string OilLookupName, string TypeCode, string TypeName, DateTime TransactionDate,
        decimal QuantityLiters, string? Reference, string? Supplier, string? Notes);

    private sealed record OilRow(int OilLookupId, string OilLookupName, decimal CurrentStock);
    private sealed record AnchorRow(int OilLookupId, DateOnly InventoryDate, decimal ActualStock);

    public async Task<OilLedgerMonthDto> GetMonthlyLedgerAsync(
        int year, int month, int? oilLookupId, CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);
        var siteIds = await GetScopedSubStationIdsAsync(cancellationToken);
        if (siteIds.Count == 0) return new OilLedgerMonthDto(year, month, []);

        var scope = await commercialMode.GetScopeAsync(cancellationToken);
        var timeZone = await GetTimeZoneAsync(scope?.StationId, cancellationToken);
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);
        var rows = await GetTransactionsAsync(siteIds, oilLookupId, cancellationToken);
        var oilRows = await GetOilRowsAsync(siteIds, oilLookupId, cancellationToken);
        // The ledger opens on day one. A later physical inventory belongs to a later
        // C5 stocktake anchor; it must not rewrite this month's opening balance.
        var anchors = await GetAnchorsAsync(siteIds, oilLookupId, start.AddDays(1), cancellationToken);

        var result = new List<OilLedgerDayDto>();
        foreach (var oil in oilRows)
        {
            var anchor = anchors.Where(x => x.OilLookupId == oil.OilLookupId)
                .OrderByDescending(x => x.InventoryDate).FirstOrDefault();
            var opening = anchor is null ? OilBalanceCalculator.Round(oil.CurrentStock) : anchor.ActualStock;
            var effectiveFrom = anchor?.InventoryDate ?? start;

            foreach (var transaction in rows.Where(x => x.OilLookupId == oil.OilLookupId)
                         .OrderBy(x => x.TransactionDate).ThenBy(x => x.Id))
            {
                var businessDate = ToBusinessDate(transaction.TransactionDate, timeZone);
                if (businessDate < effectiveFrom || businessDate >= start) continue;
                opening = Apply(opening, EffectiveType(transaction, siteIds), transaction.QuantityLiters);
            }

            for (var date = start; date < end; date = date.AddDays(1))
            {
                decimal inbound = 0, outbound = 0, transferIn = 0, transferOut = 0, loss = 0;
                foreach (var transaction in rows.Where(x => x.OilLookupId == oil.OilLookupId && ToBusinessDate(x.TransactionDate, timeZone) == date))
                {
                    if (date < effectiveFrom) continue;
                    switch (EffectiveType(transaction, siteIds))
                    {
                        case "Purchase": inbound += transaction.QuantityLiters; break;
                        case "Sale": outbound += transaction.QuantityLiters; break;
                        case "TransferIn": transferIn += transaction.QuantityLiters; break;
                        case "TransferOut": transferOut += transaction.QuantityLiters; break;
                        case "Loss": loss += transaction.QuantityLiters; break;
                    }
                }

                var closing = OilBalanceCalculator.CalculateClosing(opening, inbound, outbound, transferIn, transferOut, loss);
                result.Add(new OilLedgerDayDto(date, oil.OilLookupId, oil.OilLookupName,
                    opening, OilBalanceCalculator.Round(inbound), OilBalanceCalculator.Round(outbound),
                    OilBalanceCalculator.Round(transferIn), OilBalanceCalculator.Round(transferOut),
                    OilBalanceCalculator.Round(loss), closing));
                opening = closing;
            }
        }

        return new OilLedgerMonthDto(year, month, result);
    }

    public async Task<IReadOnlyList<OilLedgerTransactionDto>> GetDayTransactionsAsync(
        DateOnly date, int? oilLookupId, CancellationToken cancellationToken = default)
    {
        var siteIds = await GetScopedSubStationIdsAsync(cancellationToken);
        if (siteIds.Count == 0) return [];
        var scope = await commercialMode.GetScopeAsync(cancellationToken);
        var timeZone = await GetTimeZoneAsync(scope?.StationId, cancellationToken);

        return (await GetTransactionsAsync(siteIds, oilLookupId, cancellationToken))
            .Where(x => ToBusinessDate(x.TransactionDate, timeZone) == date)
            .OrderBy(x => x.TransactionDate).ThenBy(x => x.Id)
            .Select(x => new OilLedgerTransactionDto(x.Id, x.TransactionDate, x.OilLookupId, x.OilLookupName,
                EffectiveType(x, siteIds), x.TypeName, x.QuantityLiters, x.Reference, x.Supplier, x.Notes))
            .ToList();
    }

    private async Task<List<int>> GetScopedSubStationIdsAsync(CancellationToken ct)
    {
        if (await commercialMode.IsCommercialModeAsync(ct))
        {
            var scope = await commercialMode.GetScopeAsync(ct);
            return scope is null ? [] : [scope.SubStationId];
        }

        if (currentUser.IsAdmin)
            return await db.SubStations.AsNoTracking().Select(x => x.Id).ToListAsync(ct);
        return currentUser.UserId.HasValue
            ? await scopeFilter.GetAccessibleSubStationIdsAsync(currentUser.UserId.Value)
            : [];
    }

    private async Task<List<TransactionRow>> GetTransactionsAsync(List<int> siteIds, int? oilLookupId, CancellationToken ct)
    {
        var raw = await db.OilTransactions.AsNoTracking()
            .Where(x => x.IsApproved && x.CorrectedFromId == null &&
                (siteIds.Contains(x.SubStationId) || (x.DestinationSubStationId.HasValue && siteIds.Contains(x.DestinationSubStationId.Value))) &&
                (!oilLookupId.HasValue || x.OilLookupId == oilLookupId.Value))
            .Select(x => new { x.Id, x.SubStationId, x.DestinationSubStationId, x.OilLookupId,
                OilLookupName = x.OilLookup.Name, TypeCode = x.TransactionTypeLookup.Code,
                TypeName = x.TransactionTypeLookup.Name, x.TransactionDate,
                x.QuantityLiters, x.Reference, x.SupplierCompany, x.Notes })
            .ToListAsync(ct);
        return raw.Select(x => new TransactionRow(x.Id, x.SubStationId, x.DestinationSubStationId, x.OilLookupId,
            x.OilLookupName, x.TypeCode, x.TypeName, x.TransactionDate, OilBalanceCalculator.ToDecimal(x.QuantityLiters),
            x.Reference, x.SupplierCompany, x.Notes)).ToList();
    }

    private async Task<List<OilRow>> GetOilRowsAsync(List<int> siteIds, int? oilLookupId, CancellationToken ct)
    {
        var raw = await db.Oils.AsNoTracking()
            .Where(x => x.SubStationId.HasValue && siteIds.Contains(x.SubStationId.Value) &&
                (!oilLookupId.HasValue || x.OilLookupId == oilLookupId.Value))
            .Select(x => new { x.OilLookupId, x.OilLookup.Name, x.CurrentStock })
            .ToListAsync(ct);
        return raw.GroupBy(x => new { x.OilLookupId, x.Name })
            .Select(x => new OilRow(x.Key.OilLookupId, x.Key.Name, x.Sum(v => v.CurrentStock))).ToList();
    }

    private async Task<List<AnchorRow>> GetAnchorsAsync(
        List<int> siteIds, int? oilLookupId, DateOnly before, CancellationToken ct)
    {
        var beforeDate = before.ToDateTime(TimeOnly.MinValue);
        var raw = await db.PeriodicInventoryOils.AsNoTracking()
            .Where(x => x.PeriodicInventory.IsApproved && siteIds.Contains(x.PeriodicInventory.SubStationId) &&
                x.PeriodicInventory.InventoryDate < beforeDate && (!oilLookupId.HasValue || x.OilLookupId == oilLookupId.Value))
            .Select(x => new { x.OilLookupId, x.PeriodicInventory.InventoryDate, x.ActualStockLiters })
            .ToListAsync(ct);
        return raw.Select(x => new AnchorRow(x.OilLookupId, DateOnly.FromDateTime(x.InventoryDate), OilBalanceCalculator.ToDecimal(x.ActualStockLiters))).ToList();
    }

    private static decimal Apply(decimal opening, string type, decimal quantity) => type switch
    {
        "Purchase" => OilBalanceCalculator.CalculateClosing(opening, quantity, 0, 0, 0, 0),
        "TransferIn" => OilBalanceCalculator.CalculateClosing(opening, 0, 0, quantity, 0, 0),
        "Sale" => OilBalanceCalculator.CalculateClosing(opening, 0, quantity, 0, 0, 0),
        "TransferOut" => OilBalanceCalculator.CalculateClosing(opening, 0, 0, 0, quantity, 0),
        "Loss" => OilBalanceCalculator.CalculateClosing(opening, 0, 0, 0, 0, quantity),
        _ => opening
    };

    private static string EffectiveType(TransactionRow row, List<int> siteIds) =>
        row.DestinationSubStationId.HasValue && siteIds.Contains(row.DestinationSubStationId.Value) && !siteIds.Contains(row.SubStationId)
            ? "TransferIn"
            : row.TypeCode;

    private async Task<TimeZoneInfo> GetTimeZoneAsync(int? stationId, CancellationToken ct)
    {
        var id = stationId.HasValue
            ? await db.StationOperationalSettings.AsNoTracking().Where(x => x.StationId == stationId.Value).Select(x => x.TimeZone).SingleOrDefaultAsync(ct)
            : null;
        try { return TimeZoneInfo.FindSystemTimeZoneById(id ?? StationOperationalSettings.DefaultTimeZone); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }

    private static DateOnly ToBusinessDate(DateTime value, TimeZoneInfo zone)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, zone));
    }

    private static void ValidatePeriod(int year, int month)
    {
        if (year is < 2000 or > 2100) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
    }
}
