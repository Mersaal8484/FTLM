using System.Security.Claims;
using FuelTrack.Application;
using FuelTrack.Application.DTOs.FuelMovements;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class StockSummaryService : IStockSummaryService
{
    private readonly FuelTrackDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserScopeFilterService _scopeFilter;

    public StockSummaryService(
        FuelTrackDbContext db,
        IUserScopeFilterService scopeFilter,
        ICurrentUserService currentUser)
    {
        _db = db;
        _scopeFilter = scopeFilter;
        _currentUser = currentUser;
    }

    public async Task<List<FuelStockSummaryDto>> GetFuelStockSummaryAsync(DateTime fromDate, DateTime toDate, int? userId)
    {
        return await ComputeFuelStockAsync(fromDate, toDate, userId);
    }

    public async Task<List<OilStockSummaryDto>> GetOilStockSummaryAsync(DateTime fromDate, DateTime toDate, int? userId)
    {
        return await ComputeOilStockAsync(fromDate, toDate, userId);
    }

    private async Task<List<FuelStockSummaryDto>> ComputeFuelStockAsync(DateTime fromDate, DateTime toDate, int? userId)
    {
        var accessibleIds = await GetAccessibleSubStationIdsAsync(userId);

        var tanksData = await GetTanksDataAsync(accessibleIds);
        var (inventoryData, effectiveFromBySite) = await GetFuelInventoryDataAsync(accessibleIds, fromDate, toDate);
        var rawTxData = await GetFuelTransactionsRawAsync(accessibleIds, toDate);
        var filteredTxData = FilterTransactionsByEffectiveFrom(rawTxData, effectiveFromBySite, fromDate);
        var consumedData = await GetFuelConsumedDataAsync(accessibleIds, fromDate, toDate, effectiveFromBySite);

        var grouped = GroupFuelTransactions(filteredTxData, accessibleIds, effectiveFromBySite, fromDate);
        var inboundMap = grouped.Where(x => x.TypeCode == "Purchase").ToDictionary(x => (x.SubStationId, x.LookupId), x => x.Total);
        var outboundMap = grouped.Where(x => x.TypeCode == "Sale").ToDictionary(x => (x.SubStationId, x.LookupId), x => x.Total);
        var transferOutMap = grouped.Where(x => x.TypeCode == "TransferOut").ToDictionary(x => (x.SubStationId, x.LookupId), x => x.Total);
        var transferInMap = grouped.Where(x => x.TypeCode == "TransferIn").ToDictionary(x => (x.SubStationId, x.LookupId), x => x.Total);
        var lossMap = grouped.Where(x => x.TypeCode == "Loss").ToDictionary(x => (x.SubStationId, x.LookupId), x => x.Total);
        var consumedMap = consumedData.ToDictionary(x => (x.SubStationId, x.LookupId), x => x.Consumed);
        var baseStockMap = BuildBaseStockMap(inventoryData, tanksData);
        var tankActualMap = tanksData.ToDictionary(x => (x.SubStationId, x.FuelLookupId), x => x.TotalActualStock);

        var results = new List<FuelStockSummaryDto>();

        foreach (var tankRow in tanksData)
        {
            var key = (tankRow.SubStationId, tankRow.FuelLookupId);

            var inbound = inboundMap.GetValueOrDefault(key, 0);
            var outbound = outboundMap.GetValueOrDefault(key, 0);
            var transferOut = transferOutMap.GetValueOrDefault(key, 0);
            var transferIn = transferInMap.GetValueOrDefault(key, 0);
            var loss = lossMap.GetValueOrDefault(key, 0);
            var consumed = consumedMap.GetValueOrDefault(key, 0);
            var baseStock = baseStockMap.GetValueOrDefault(key, tankRow.TotalActualStock);

            var remaining = (baseStock + inbound + transferIn) - (transferOut + loss + consumed);
            var available = Math.Max(0, remaining - tankRow.TotalDeadStock);
            var alertLevel = DetermineAlertLevel(remaining, tankRow.TotalMinAlert);

            results.Add(new FuelStockSummaryDto(
                tankRow.SubStationId,
                tankRow.SubStationName,
                tankRow.StationId,
                tankRow.StationName,
                tankRow.BranchId,
                tankRow.BranchName,
                tankRow.FuelLookupId,
                tankRow.FuelLookupName,
                tankRow.TotalCapacity,
                tankRow.TotalMinAlert,
                tankRow.TotalDeadStock,
                tankRow.TotalActualStock,
                baseStock,
                inbound,
                outbound,
                transferOut,
                transferIn,
                loss,
                consumed,
                remaining,
                available,
                alertLevel));
        }

        return results;
    }

    private async Task<List<OilStockSummaryDto>> ComputeOilStockAsync(DateTime fromDate, DateTime toDate, int? userId)
    {
        var accessibleIds = await GetAccessibleSubStationIdsAsync(userId);

        var (inventoryData, effectiveFromBySite) = await GetOilInventoryDataAsync(accessibleIds, fromDate, toDate);
        var txData = await GetOilTransactionsRawAsync(accessibleIds, toDate);
        var filteredTxData = FilterTransactionsByEffectiveFrom(txData, effectiveFromBySite, fromDate);
        var consumedData = await GetOilConsumedDataAsync(accessibleIds, fromDate, toDate, effectiveFromBySite);
        var subStationInfo = await GetSubStationInfoAsync(accessibleIds);
        var oilLookupNames = await GetOilLookupNamesAsync();
        var oilsData = await GetOilsDataAsync(accessibleIds);
        var validOilLookupIds = new HashSet<int>(oilLookupNames.Keys);

        var grouped = GroupOilTransactions(filteredTxData, accessibleIds, effectiveFromBySite, fromDate);
        var inboundMap = grouped.Where(x => x.TypeCode == "Purchase").ToDictionary(x => (x.SubStationId, x.LookupId), x => x.Total);
        var outboundMap = grouped.Where(x => x.TypeCode == "Sale").ToDictionary(x => (x.SubStationId, x.LookupId), x => x.Total);
        var transferOutMap = grouped.Where(x => x.TypeCode == "TransferOut").ToDictionary(x => (x.SubStationId, x.LookupId), x => x.Total);
        var transferInMap = grouped.Where(x => x.TypeCode == "TransferIn").ToDictionary(x => (x.SubStationId, x.LookupId), x => x.Total);
        var lossMap = grouped.Where(x => x.TypeCode == "Loss").ToDictionary(x => (x.SubStationId, x.LookupId), x => x.Total);
        var consumedMap = consumedData.ToDictionary(x => (x.SubStationId, x.LookupId), x => x.Consumed);
        var baseStockMap = inventoryData.ToDictionary(x => (x.SubStationId, x.LookupId), x => x.ActualStockLiters);

        var allKeys = new HashSet<(int, int)>(
            filteredTxData.Select(x => (x.SubStationId, x.FuelLookupId))
                .Concat(consumedData.Select(x => (x.SubStationId, x.LookupId)))
                .Concat(inventoryData.Select(x => (x.SubStationId, x.LookupId)))
                .Concat(oilsData.Select(o => (o.SubStationId, o.OilLookupId)))
                .Where(k => validOilLookupIds.Contains(k.Item2)));

        var results = new List<OilStockSummaryDto>();

        foreach (var key in allKeys)
        {
            var subStationId = key.Item1;
            var oilLookupId = key.Item2;

            var info = subStationInfo.GetValueOrDefault(subStationId);
            var subStationName = info?.SubStationName ?? "—";
            var stationName = info?.StationName ?? "—";
            var branchName = info?.BranchName ?? "—";
            var stationId = info?.StationId ?? 0;
            var branchId = info?.BranchId ?? 0;

            var inbound = inboundMap.GetValueOrDefault(key, 0);
            var outbound = outboundMap.GetValueOrDefault(key, 0);
            var transferOut = transferOutMap.GetValueOrDefault(key, 0);
            var transferIn = transferInMap.GetValueOrDefault(key, 0);
            var loss = lossMap.GetValueOrDefault(key, 0);
            var consumed = consumedMap.GetValueOrDefault(key, 0);
            var baseStock = baseStockMap.GetValueOrDefault(key, 0);

            if (baseStock == 0)
            {
                var oil = oilsData.FirstOrDefault(o => o.SubStationId == subStationId && o.OilLookupId == oilLookupId);
                if (oil != null)
                {
                    baseStock = (double)oil.CurrentStock;
                }
            }

            var remaining = (baseStock + inbound + transferIn) - (transferOut + loss + consumed);
            var alertLevel = DetermineOilAlertLevel(remaining);

            results.Add(new OilStockSummaryDto(
                subStationId,
                subStationName,
                stationId,
                stationName,
                branchId,
                branchName,
                oilLookupId,
                oilLookupNames.GetValueOrDefault(oilLookupId, "—"),
                baseStock,
                inbound,
                outbound,
                transferOut,
                transferIn,
                loss,
                consumed,
                remaining,
                alertLevel));
        }

        return results;
    }

    private async Task<List<int>> GetAccessibleSubStationIdsAsync(int? userId)
    {
        if (!userId.HasValue)
            return new List<int>();

        if (_currentUser.HasAnyAdminRole)
            return await _db.SubStations
                .Where(s => s.DeletedAt == null)
                .Select(s => s.Id)
                .ToListAsync();

        var ids = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
        return ids;
    }

    private async Task<List<TankGroupDto>> GetTanksDataAsync(List<int> accessibleIds)
    {
        return await _db.FuelTanks
            .Where(t => accessibleIds.Contains(t.SubStationId) && t.DeletedAt == null)
            .GroupBy(t => new { t.SubStationId, t.FuelLookupId })
            .Select(g => new TankGroupDto
            {
                SubStationId = g.Key.SubStationId,
                FuelLookupId = g.Key.FuelLookupId,
                TotalCapacity = g.Sum(t => t.CapacityLiters),
                TotalMinAlert = g.Sum(t => (double?)t.MinAlertLiters) ?? 0,
                TotalDeadStock = g.Sum(t => (double?)t.DeadStockLiters) ?? 0,
                TotalActualStock = g.Sum(t => (double?)t.ActualStockLiters) ?? 0,
                SubStationName = g.First().SubStation.Name,
                StationId = g.First().SubStation.StationId,
                StationName = g.First().SubStation.Station.Name,
                BranchId = g.First().SubStation.Station.BranchId,
                BranchName = g.First().SubStation.Station.Branch.Name,
                FuelLookupName = g.First().FuelLookup.Name
            })
            .ToListAsync();
    }

    private async Task<List<TransactionRawDto>> GetFuelTransactionsRawAsync(List<int> accessibleIds, DateTime toDate)
    {
        return await _db.FuelTransactions
            .Where(ft => (accessibleIds.Contains(ft.SubStationId) || (ft.DestinationSubStationId.HasValue && accessibleIds.Contains(ft.DestinationSubStationId.Value)))
                      && ft.IsApproved
                      && ft.DeletedAt == null
                      && ft.TransactionDate <= toDate)
            .Select(ft => new TransactionRawDto
            {
                SubStationId = ft.SubStationId,
                DestinationSubStationId = ft.DestinationSubStationId,
                FuelLookupId = ft.FuelLookupId,
                Code = ft.TransactionTypeLookup.Code,
                QuantityLiters = ft.QuantityLiters,
                TransactionDate = ft.TransactionDate
            })
            .ToListAsync();
    }

    private async Task<List<TransactionRawDto>> GetOilTransactionsRawAsync(List<int> accessibleIds, DateTime toDate)
    {
        return await _db.OilTransactions
            .Where(ot => (accessibleIds.Contains(ot.SubStationId) || (ot.DestinationSubStationId.HasValue && accessibleIds.Contains(ot.DestinationSubStationId.Value)))
                      && ot.IsApproved
                      && ot.DeletedAt == null
                      && ot.TransactionDate <= toDate)
            .Select(ot => new TransactionRawDto
            {
                SubStationId = ot.SubStationId,
                DestinationSubStationId = ot.DestinationSubStationId,
                FuelLookupId = ot.OilLookupId,
                Code = ot.TransactionTypeLookup.Code,
                QuantityLiters = ot.QuantityLiters,
                TransactionDate = ot.TransactionDate
            })
            .ToListAsync();
    }

    private static List<TransactionRawDto> FilterTransactionsByEffectiveFrom(
        List<TransactionRawDto> rawData,
        Dictionary<int, DateTime>? effectiveFromBySite,
        DateTime defaultFromDate)
    {
        if (effectiveFromBySite == null || effectiveFromBySite.Count == 0)
            return rawData;

        return rawData
            .Where(tx =>
            {
                var sourceEffective = effectiveFromBySite.GetValueOrDefault(tx.SubStationId, defaultFromDate);

                if (!tx.DestinationSubStationId.HasValue)
                    return tx.TransactionDate >= sourceEffective;

                var destEffective = effectiveFromBySite.GetValueOrDefault(tx.DestinationSubStationId.Value, defaultFromDate);
                return tx.TransactionDate >= sourceEffective || tx.TransactionDate >= destEffective;
            })
            .ToList();
    }

    private static List<TransactionGroupDto> GroupFuelTransactions(List<TransactionRawDto> rawData, List<int> accessibleIds, Dictionary<int, DateTime>? effectiveFromBySite, DateTime defaultFromDate)
    {
        var sourceFiltered = effectiveFromBySite == null
            ? rawData.Where(x => accessibleIds.Contains(x.SubStationId))
            : rawData.Where(x => accessibleIds.Contains(x.SubStationId) && x.TransactionDate >= effectiveFromBySite.GetValueOrDefault(x.SubStationId, defaultFromDate));

        var grouped = new List<TransactionGroupDto>();

        foreach (var g in sourceFiltered.GroupBy(x => new { x.SubStationId, x.FuelLookupId, x.Code }))
        {
            var typeCode = g.Key.Code == "Transfer" ? "TransferOut" : g.Key.Code;
            grouped.Add(new TransactionGroupDto { SubStationId = g.Key.SubStationId, LookupId = g.Key.FuelLookupId, TypeCode = typeCode, Total = g.Sum(x => x.QuantityLiters) });
        }

        var destFiltered = effectiveFromBySite == null
            ? rawData.Where(x => (x.Code == "Transfer" || x.Code == "TransferOut") && x.DestinationSubStationId.HasValue && accessibleIds.Contains(x.DestinationSubStationId.Value))
            : rawData.Where(x => (x.Code == "Transfer" || x.Code == "TransferOut") && x.DestinationSubStationId.HasValue && accessibleIds.Contains(x.DestinationSubStationId.Value) && x.TransactionDate >= effectiveFromBySite.GetValueOrDefault(x.DestinationSubStationId.Value, defaultFromDate));

        foreach (var g in destFiltered.GroupBy(x => new { SubStationId = x.DestinationSubStationId.Value, x.FuelLookupId }))
        {
            grouped.Add(new TransactionGroupDto { SubStationId = g.Key.SubStationId, LookupId = g.Key.FuelLookupId, TypeCode = "TransferIn", Total = g.Sum(x => x.QuantityLiters) });
        }

        return grouped;
    }

    private static List<TransactionGroupDto> GroupOilTransactions(List<TransactionRawDto> rawData, List<int> accessibleIds, Dictionary<int, DateTime>? effectiveFromBySite, DateTime defaultFromDate)
    {
        var sourceFiltered = effectiveFromBySite == null
            ? rawData.Where(x => accessibleIds.Contains(x.SubStationId))
            : rawData.Where(x => accessibleIds.Contains(x.SubStationId) && x.TransactionDate >= effectiveFromBySite.GetValueOrDefault(x.SubStationId, defaultFromDate));

        var grouped = new List<TransactionGroupDto>();

        foreach (var g in sourceFiltered.GroupBy(x => new { x.SubStationId, x.FuelLookupId, x.Code }))
        {
            var typeCode = g.Key.Code == "Transfer" ? "TransferOut" : g.Key.Code;
            grouped.Add(new TransactionGroupDto { SubStationId = g.Key.SubStationId, LookupId = g.Key.FuelLookupId, TypeCode = typeCode, Total = g.Sum(x => x.QuantityLiters) });
        }

        var destFiltered = effectiveFromBySite == null
            ? rawData.Where(x => (x.Code == "Transfer" || x.Code == "TransferOut") && x.DestinationSubStationId.HasValue && accessibleIds.Contains(x.DestinationSubStationId.Value))
            : rawData.Where(x => (x.Code == "Transfer" || x.Code == "TransferOut") && x.DestinationSubStationId.HasValue && accessibleIds.Contains(x.DestinationSubStationId.Value) && x.TransactionDate >= effectiveFromBySite.GetValueOrDefault(x.DestinationSubStationId.Value, defaultFromDate));

        foreach (var g in destFiltered.GroupBy(x => new { SubStationId = x.DestinationSubStationId.Value, x.FuelLookupId }))
        {
            grouped.Add(new TransactionGroupDto { SubStationId = g.Key.SubStationId, LookupId = g.Key.FuelLookupId, TypeCode = "TransferIn", Total = g.Sum(x => x.QuantityLiters) });
        }

        return grouped;
    }

    private async Task<List<ConsumedGroupDto>> GetFuelConsumedDataAsync(List<int> accessibleIds, DateTime fromDate, DateTime toDate, Dictionary<int, DateTime>? effectiveFromBySite)
    {
        var readings = await _db.DailyReadings
            .Where(dr => dr.ReadingDate <= toDate
                      && accessibleIds.Contains(dr.Generator.SubStationId)
                      && dr.DeletedAt == null
                      && dr.ReadingStatusLookup.Code == "Approved")
            .Select(dr => new
            {
                dr.Generator.SubStationId,
                dr.FuelLookupId,
                dr.ReadingDate,
                Consumed = Math.Floor(
                    dr.StandardFuelRate > 0 && (dr.CurrentEnergyKWh - dr.PrevEnergyKWh) > 0
                        ? (double)(dr.CurrentEnergyKWh - dr.PrevEnergyKWh) * (double)dr.StandardFuelRate
                        : 0)
            })
            .ToListAsync();

        var filtered = effectiveFromBySite == null || effectiveFromBySite.Count == 0
            ? readings.Where(dr => dr.ReadingDate >= fromDate)
            : readings.Where(dr => dr.ReadingDate >= effectiveFromBySite.GetValueOrDefault(dr.SubStationId, fromDate));

        return filtered
            .GroupBy(dr => new { dr.SubStationId, dr.FuelLookupId })
            .Select(g => new ConsumedGroupDto
            {
                SubStationId = g.Key.SubStationId,
                LookupId = g.Key.FuelLookupId,
                Consumed = g.Sum(dr => dr.Consumed)
            })
            .ToList();
    }

    private async Task<List<ConsumedGroupDto>> GetOilConsumedDataAsync(List<int> accessibleIds, DateTime fromDate, DateTime toDate, Dictionary<int, DateTime>? effectiveFromBySite)
    {
        var readings = await _db.DailyReadings
            .Where(dr => dr.ReadingDate <= toDate
                      && accessibleIds.Contains(dr.Generator.SubStationId)
                      && dr.DeletedAt == null
                      && dr.ReadingStatusLookup.Code == "Approved")
            .Select(dr => new
            {
                dr.Generator.SubStationId,
                dr.OilLookupId,
                dr.ReadingDate,
                dr.OilUsedLiters
            })
            .ToListAsync();

        var filtered = effectiveFromBySite == null || effectiveFromBySite.Count == 0
            ? readings.Where(dr => dr.ReadingDate >= fromDate)
            : readings.Where(dr => dr.ReadingDate >= effectiveFromBySite.GetValueOrDefault(dr.SubStationId, fromDate));

        return filtered
            .GroupBy(dr => new { dr.SubStationId, dr.OilLookupId })
            .Select(g => new ConsumedGroupDto
            {
                SubStationId = g.Key.SubStationId,
                LookupId = g.Key.OilLookupId,
                Consumed = g.Sum(dr => dr.OilUsedLiters)
            })
            .ToList();
    }

    private async Task<(List<InventoryGroupDto> inventories, Dictionary<int, DateTime> effectiveFromBySite)> 
        GetFuelInventoryDataAsync(List<int> accessibleIds, DateTime fromDate, DateTime toDate)
    {
        var inventories = new List<InventoryGroupDto>();
        var effectiveFromBySite = new Dictionary<int, DateTime>();

        var allInventoryFuels = await _db.PeriodicInventoryFuels
            .Where(pif => pif.PeriodicInventory.IsApproved
                       && pif.PeriodicInventory.InventoryDate <= toDate
                       && accessibleIds.Contains(pif.PeriodicInventory.SubStationId))
            .Select(pif => new
            {
                pif.PeriodicInventory.SubStationId,
                pif.FuelLookupId,
                pif.PeriodicInventory.InventoryDate,
                pif.ActualStockLiters
            })
            .ToListAsync();

        var inPeriod = allInventoryFuels
            .Where(x => x.InventoryDate >= fromDate)
            .GroupBy(x => new { x.SubStationId, x.FuelLookupId })
            .Select(g => new InventoryGroupDto
            {
                SubStationId = g.Key.SubStationId,
                LookupId = g.Key.FuelLookupId,
                ActualStockLiters = g.OrderByDescending(x => x.InventoryDate).First().ActualStockLiters
            })
            .ToList();

        foreach (var item in inPeriod)
        {
            inventories.Add(item);
            var invDate = allInventoryFuels
                .Where(x => x.SubStationId == item.SubStationId && x.FuelLookupId == item.LookupId && x.InventoryDate >= fromDate)
                .Max(x => x.InventoryDate);
            effectiveFromBySite[item.SubStationId] = invDate.AddDays(1);
        }

        var sitesWithInPeriod = new HashSet<int>(inPeriod.Select(x => x.SubStationId));
        var remainingSites = accessibleIds.Except(sitesWithInPeriod).ToList();

        if (remainingSites.Any())
        {
            var prePeriod = allInventoryFuels
                .Where(x => x.InventoryDate < fromDate && remainingSites.Contains(x.SubStationId))
                .GroupBy(x => new { x.SubStationId, x.FuelLookupId })
                .Select(g => new InventoryGroupDto
                {
                    SubStationId = g.Key.SubStationId,
                    LookupId = g.Key.FuelLookupId,
                    ActualStockLiters = g.OrderByDescending(x => x.InventoryDate).First().ActualStockLiters
                })
                .ToList();

            inventories.AddRange(prePeriod);
            foreach (var siteId in remainingSites)
            {
                effectiveFromBySite[siteId] = fromDate;
            }
        }

        foreach (var siteId in accessibleIds)
        {
            if (!effectiveFromBySite.ContainsKey(siteId))
                effectiveFromBySite[siteId] = fromDate;
        }

        return (inventories, effectiveFromBySite);
    }

    private async Task<(List<InventoryGroupDto> inventories, Dictionary<int, DateTime> effectiveFromBySite)> 
        GetOilInventoryDataAsync(List<int> accessibleIds, DateTime fromDate, DateTime toDate)
    {
        var inventories = new List<InventoryGroupDto>();
        var effectiveFromBySite = new Dictionary<int, DateTime>();

        var allInventoryOils = await _db.PeriodicInventoryOils
            .Where(pio => pio.PeriodicInventory.IsApproved
                       && pio.PeriodicInventory.InventoryDate <= toDate
                       && accessibleIds.Contains(pio.PeriodicInventory.SubStationId))
            .Select(pio => new
            {
                pio.PeriodicInventory.SubStationId,
                pio.OilLookupId,
                pio.PeriodicInventory.InventoryDate,
                pio.ActualStockLiters
            })
            .ToListAsync();

        var inPeriod = allInventoryOils
            .Where(x => x.InventoryDate >= fromDate)
            .GroupBy(x => new { x.SubStationId, x.OilLookupId })
            .Select(g => new InventoryGroupDto
            {
                SubStationId = g.Key.SubStationId,
                LookupId = g.Key.OilLookupId,
                ActualStockLiters = g.OrderByDescending(x => x.InventoryDate).First().ActualStockLiters
            })
            .ToList();

        foreach (var item in inPeriod)
        {
            inventories.Add(item);
            var invDate = allInventoryOils
                .Where(x => x.SubStationId == item.SubStationId && x.OilLookupId == item.LookupId && x.InventoryDate >= fromDate)
                .Max(x => x.InventoryDate);
            effectiveFromBySite[item.SubStationId] = invDate.AddDays(1);
        }

        var sitesWithInPeriod = new HashSet<int>(inPeriod.Select(x => x.SubStationId));
        var remainingSites = accessibleIds.Except(sitesWithInPeriod).ToList();

        if (remainingSites.Any())
        {
            var prePeriod = allInventoryOils
                .Where(x => x.InventoryDate < fromDate && remainingSites.Contains(x.SubStationId))
                .GroupBy(x => new { x.SubStationId, x.OilLookupId })
                .Select(g => new InventoryGroupDto
                {
                    SubStationId = g.Key.SubStationId,
                    LookupId = g.Key.OilLookupId,
                    ActualStockLiters = g.OrderByDescending(x => x.InventoryDate).First().ActualStockLiters
                })
                .ToList();

            inventories.AddRange(prePeriod);
            foreach (var siteId in remainingSites)
            {
                effectiveFromBySite[siteId] = fromDate;
            }
        }

        foreach (var siteId in accessibleIds)
        {
            if (!effectiveFromBySite.ContainsKey(siteId))
                effectiveFromBySite[siteId] = fromDate;
        }

        return (inventories, effectiveFromBySite);
    }

    private async Task<Dictionary<int, SubStationInfoDto>> GetSubStationInfoAsync(List<int> accessibleIds)
    {
        var data = await _db.SubStations
            .Where(s => accessibleIds.Contains(s.Id) && s.DeletedAt == null)
            .Select(s => new SubStationInfoDto
            {
                SubStationId = s.Id,
                SubStationName = s.Name,
                StationId = s.StationId,
                StationName = s.Station.Name,
                BranchId = s.Station.BranchId,
                BranchName = s.Station.Branch.Name
            })
            .ToListAsync();

        return data.ToDictionary(x => x.SubStationId);
    }

    private async Task<Dictionary<int, string>> GetOilLookupNamesAsync()
    {
        var data = await _db.Lookups
            .Where(l => l.Category.Name == "OilType" && l.DeletedAt == null)
            .Select(l => new { l.Id, l.Name })
            .ToListAsync();

        return data.ToDictionary(x => x.Id, x => x.Name);
    }

    private async Task<List<OilGroupDto>> GetOilsDataAsync(List<int> accessibleIds)
    {
        return await _db.Oils
            .Where(o => o.SubStationId.HasValue
                     && accessibleIds.Contains(o.SubStationId.Value)
                     && o.DeletedAt == null)
            .GroupBy(o => new { SubStationId = o.SubStationId.Value, o.OilLookupId })
            .Select(g => new OilGroupDto
            {
                SubStationId = g.Key.SubStationId,
                OilLookupId = g.Key.OilLookupId,
                CurrentStock = g.Sum(o => (double)o.CurrentStock)
            })
            .ToListAsync();
    }

    private static Dictionary<(int, int), double> BuildBaseStockMap(List<InventoryGroupDto> inventoryData, List<TankGroupDto> tanksData)
    {
        var map = inventoryData
            .GroupBy(x => (x.SubStationId, x.LookupId))
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.ActualStockLiters).First().ActualStockLiters,
                EqualityComparer<(int, int)>.Default);

        foreach (var tank in tanksData)
        {
            var key = (tank.SubStationId, tank.FuelLookupId);
            if (!map.ContainsKey(key))
                map[key] = tank.TotalActualStock;
        }

        return map;
    }

    private static string DetermineAlertLevel(double remaining, double minAlert)
    {
        if (remaining <= 0) return "danger";
        if (remaining <= minAlert) return "danger";
        if (remaining <= minAlert * 1.2) return "warning";
        return "normal";
    }

    private static string DetermineOilAlertLevel(double remaining)
    {
        if (remaining <= 0) return "danger";
        if (remaining <= 100) return "danger";
        if (remaining <= 200) return "warning";
        return "normal";
    }

    private class TankGroupDto
    {
        public int SubStationId { get; set; }
        public int FuelLookupId { get; set; }
        public double TotalCapacity { get; set; }
        public double TotalMinAlert { get; set; }
        public double TotalDeadStock { get; set; }
        public double TotalActualStock { get; set; }
        public string SubStationName { get; set; } = string.Empty;
        public int StationId { get; set; }
        public string StationName { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string FuelLookupName { get; set; } = string.Empty;
    }

    private class TransactionRawDto
    {
        public int SubStationId { get; set; }
        public int? DestinationSubStationId { get; set; }
        public int FuelLookupId { get; set; }
        public string Code { get; set; } = string.Empty;
        public double QuantityLiters { get; set; }
        public DateTime TransactionDate { get; set; }
    }

    private class TransactionGroupDto
    {
        public int SubStationId { get; set; }
        public int LookupId { get; set; }
        public string TypeCode { get; set; } = string.Empty;
        public double Total { get; set; }
    }

    private class ConsumedGroupDto
    {
        public int SubStationId { get; set; }
        public int LookupId { get; set; }
        public double Consumed { get; set; }
    }

    private class InventoryGroupDto
    {
        public int SubStationId { get; set; }
        public int LookupId { get; set; }
        public double ActualStockLiters { get; set; }
    }

    private class SubStationInfoDto
    {
        public int SubStationId { get; set; }
        public string SubStationName { get; set; } = string.Empty;
        public int StationId { get; set; }
        public string StationName { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
    }

    private class OilGroupDto
    {
        public int SubStationId { get; set; }
        public int OilLookupId { get; set; }
        public double CurrentStock { get; set; }
    }
}
