using System.Security.Claims;
using FuelTrack.Application;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Reports;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly FuelTrackDbContext _db;
    private readonly ICacheService _cache;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;

    public ReportService(FuelTrackDbContext db, ICacheService cache, ICurrentUserService currentUser, IUserScopeFilterService scopeFilter)
    {
        _db = db;
        _cache = cache;
        _currentUser = currentUser;
        _scopeFilter = scopeFilter;
    }

    private static (DateTime utcFrom, DateTime utcTo) ConvertToUtcRange(DateTime? dateFrom, DateTime? dateTo, string timeZoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var utcFrom = dateFrom.HasValue
            ? TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dateFrom.Value, DateTimeKind.Unspecified), tz)
            : DateTime.UtcNow.AddDays(-30);
        var utcTo = dateTo.HasValue
            ? TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dateTo.Value.AddDays(1), DateTimeKind.Unspecified), tz)
            : DateTime.UtcNow;
        return (utcFrom, utcTo);
    }

    private async Task<HashSet<int>> GetAccessibleSubStationIdsAsync()
    {
        if (_currentUser.HasAnyAdminRole) return new HashSet<int>();
        var userId = _currentUser.UserId;
        if (!userId.HasValue) return new HashSet<int>();
        var ids = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
        return new HashSet<int>(ids);
    }

    public async Task<DashboardStatsResponseDto> GetDashboardStatsAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo, string timeZoneId = "Asia/Riyadh")
    {
        var (utcFrom, utcTo) = ConvertToUtcRange(dateFrom, dateTo, timeZoneId);
        var userScopeKey = _currentUser.HasAnyAdminRole ? "admin" : $"u{_currentUser.UserId ?? 0}";
        var cacheKey = $"dashboard:stats:{userScopeKey}:{branchId}:{stationId}:{subStationId}:{utcFrom:yyyyMMdd}:{utcTo:yyyyMMdd}";
        var cached = await _cache.GetAsync<DashboardStatsResponseDto>(cacheKey);
        if (cached is not null) return cached;

        dateFrom = utcFrom;
        dateTo = utcTo;

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = !_currentUser.HasAnyAdminRole;

        var fuelTanks = await _db.FuelTanks
            .Where(ft => ft.DeletedAt == null
                && (!subStationId.HasValue || ft.SubStationId == subStationId.Value)
                && (!stationId.HasValue || ft.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || ft.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(ft.SubStationId)))
            .Select(ft => new {
                ft.Id,
                ft.Name,
                SubStationName = ft.SubStation != null ? ft.SubStation.Name : "?",
                Stock = ft.ActualStockLiters ?? 0,
                Capacity = ft.CapacityLiters,
                MinAlert = ft.MinAlertLiters,
                DeadStock = ft.DeadStockLiters
            })
            .AsNoTracking().ToListAsync();

        var fuelTx = await _db.FuelTransactions
            .Where(f => f.DeletedAt == null && f.IsApproved
                && f.TransactionDate >= dateFrom.Value && f.TransactionDate <= dateTo.Value
                && (!subStationId.HasValue || f.SubStationId == subStationId.Value)
                && (!stationId.HasValue || f.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || f.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(f.SubStationId)))
            .Select(f => new {
                TypeCode = f.TransactionTypeLookup != null ? f.TransactionTypeLookup.Code : null,
                f.QuantityLiters
            })
            .AsNoTracking().ToListAsync();

        var totalReceived = fuelTx.Where(f => f.TypeCode == "Purchase").Sum(f => f.QuantityLiters);
        var totalTransferred = fuelTx.Where(f => f.TypeCode == "TransferOut" || f.TypeCode == "Transfer").Sum(f => f.QuantityLiters);

        var tankLevels = fuelTanks.Select(ft =>
        {
            var pct = ft.Capacity > 0 ? ft.Stock / ft.Capacity * 100 : 0;
            var isLow = ft.MinAlert.HasValue && ft.Stock <= ft.MinAlert.Value;
            var isCritical = ft.DeadStock.HasValue && ft.Stock <= ft.DeadStock.Value;
            return new FuelTankLevelDto(
                ft.Id, ft.Name, ft.SubStationName,
                ft.Stock, ft.Capacity, Math.Round(pct, 1), isLow, isCritical);
        }).ToList();

        var readings = await _db.DailyReadings
            .Where(r => r.DeletedAt == null
                && r.ReadingStatusLookup != null && r.ReadingStatusLookup.Code == "Approved"
                && r.ReadingDate >= dateFrom.Value && r.ReadingDate <= dateTo.Value
                && (!subStationId.HasValue || r.Generator.SubStationId == subStationId.Value)
                && (!stationId.HasValue || r.Generator.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || r.Generator.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(r.Generator.SubStationId)))
            .Select(r => new {
                EnergyDiff = r.CurrentEnergyKWh - r.PrevEnergyKWh,
                HoursDiff = r.CurrentRunningHours - r.PrevRunningHours,
                r.StandardFuelRate
            })
            .AsNoTracking().ToListAsync();

        var totalProduced = readings.Sum(r => r.EnergyDiff);
        var totalConsumed = readings.Sum(r => (double)r.StandardFuelRate * r.EnergyDiff);
        var totalHours = readings.Sum(r => r.HoursDiff);
        var avgLoad = totalHours > 0 ? totalProduced / totalHours : 0;
        var maxLoad = readings.Any() ? readings.Max(r => r.EnergyDiff / Math.Max(r.HoursDiff, 0.01)) : 0;

        var fuel = new FuelSummary(totalReceived, totalTransferred, totalConsumed,
            totalReceived - totalTransferred - totalConsumed, tankLevels);

        var stationReadings = await _db.SubStationReadings
            .AsNoTracking()
            .Where(r => r.DeletedAt == null
                && r.ReadingStatusLookup != null && r.ReadingStatusLookup.Code == "Approved"
                && r.ReadingDate >= dateFrom.Value && r.ReadingDate <= dateTo.Value
                && (!subStationId.HasValue || r.SubStationId == subStationId.Value)
                && (!stationId.HasValue || r.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || r.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(r.SubStationId)))
            .Select(r => r.CurrentExportedEnergyKWh - r.PrevExportedEnergyKWh)
            .ToListAsync();

        var totalExported = stationReadings.Sum();
        var activeGens = await _db.Generators
            .Where(g => g.DeletedAt == null
                && g.StatusLookup != null && g.StatusLookup.Code == "Online"
                && (!subStationId.HasValue || g.SubStationId == subStationId.Value)
                && (!stationId.HasValue || g.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || g.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(g.SubStationId)))
            .AsNoTracking().CountAsync();

        var energy = new EnergySummary(totalProduced, totalExported, totalProduced - totalExported,
            Math.Round(avgLoad, 1), Math.Round(maxLoad, 1), activeGens);

        var oils = await _db.Oils
            .Where(o => o.DeletedAt == null
                && (!subStationId.HasValue || o.SubStationId == subStationId.Value)
                && (!stationId.HasValue || (o.SubStation != null && o.SubStation.StationId == stationId.Value))
                && (!branchId.HasValue || (o.SubStation != null && o.SubStation.Station.BranchId == branchId.Value))
                && (!hasScopeFilter || (o.SubStationId.HasValue && accessibleIds.Contains(o.SubStationId.Value))))
            .Select(o => new {
                o.Id,
                OilName = o.OilLookup != null ? o.OilLookup.Name : "?",
                SubStationName = o.SubStation != null ? o.SubStation.Name : "-",
                Stock = (double)o.CurrentStock,
                Min = (double)o.MinStock
            })
            .AsNoTracking().ToListAsync();

        var oilTx = await _db.OilTransactions
            .Where(o => o.DeletedAt == null && o.IsApproved
                && o.TransactionDate >= dateFrom.Value && o.TransactionDate <= dateTo.Value
                && (!subStationId.HasValue || o.SubStationId == subStationId.Value)
                && (!stationId.HasValue || o.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || o.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(o.SubStationId)))
            .Select(o => new {
                TypeCode = o.TransactionTypeLookup != null ? o.TransactionTypeLookup.Code : null,
                o.QuantityLiters
            })
            .AsNoTracking().ToListAsync();

        var oilReceived = oilTx.Where(o => o.TypeCode == "Purchase").Sum(o => o.QuantityLiters);
        var oilConsumed = oilTx.Where(o => o.TypeCode == "Consumption").Sum(o => o.QuantityLiters);

        var oilTankLevels = oils.Select(o =>
        {
            var pct = o.Min > 0 ? o.Stock / o.Min * 100 : 100;
            return new OilTankLevelDto(o.Id, o.OilName, o.SubStationName, o.Stock, o.Min, Math.Round(pct, 1));
        }).ToList();

        var oilSummary = new OilSummary(oilReceived, oilConsumed, oilTankLevels);

        var lowFuel = tankLevels.Count(t => t.IsLow && !t.IsCritical);
        var criticalFuel = tankLevels.Count(t => t.IsCritical);
        var lowOil = oilTankLevels.Count(t => t.PercentageFull < 30);
        var staleThreshold = DateTime.UtcNow.Date.AddDays(-3);
        var staleReadings = await _db.DailyReadings
            .Where(r => r.DeletedAt == null && r.ReadingDate < staleThreshold
                && (!subStationId.HasValue || r.Generator.SubStationId == subStationId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(r.Generator.SubStationId)))
            .AsNoTracking().AnyAsync() ? 1 : 0;

        var alerts = new AlertSummary(lowFuel, criticalFuel, lowOil, staleReadings,
            lowFuel + criticalFuel + lowOil + staleReadings);

        var totalBranches = await _db.Branches.CountAsync(b => b.DeletedAt == null
            && (!hasScopeFilter || b.Stations.Any(s => s.SubStations.Any(ss => accessibleIds.Contains(ss.Id)))));
        var totalStations = await _db.Stations.CountAsync(s => s.DeletedAt == null
            && (!branchId.HasValue || s.BranchId == branchId.Value)
            && (!hasScopeFilter || s.SubStations.Any(ss => accessibleIds.Contains(ss.Id))));
        var totalSubStations = await _db.SubStations.CountAsync(ss => ss.DeletedAt == null
            && (!subStationId.HasValue || ss.Id == subStationId.Value)
            && (!stationId.HasValue || ss.StationId == stationId.Value)
            && (!hasScopeFilter || accessibleIds.Contains(ss.Id)));
        var totalFuelTanks = fuelTanks.Count;
        var totalOilTanks = oils.Count;

        var allGens = await _db.Generators
            .AsNoTracking()
            .Where(g => g.DeletedAt == null
                && (!subStationId.HasValue || g.SubStationId == subStationId.Value)
                && (!stationId.HasValue || g.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || g.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(g.SubStationId)))
            .Select(g => g.StatusLookup != null ? g.StatusLookup.Code : null)
            .ToListAsync();

        var onlineGens = allGens.Count(code => code == "Online" || code == "Active");
        var offlineGens = allGens.Count(code => code == "Offline" || code == "Inactive");
        var maintenanceGens = allGens.Count(code => code == "Maintenance");

        var system = new SystemSummary(totalBranches, totalStations, totalSubStations,
            allGens.Count, totalFuelTanks, totalOilTanks, onlineGens, offlineGens, maintenanceGens);

        var result = new DashboardStatsResponseDto(fuel, energy, oilSummary, alerts, system);
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));
        return result;
    }

    public async Task<FuelOilDashboardDto> GetFuelOilDashboardAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo, string timeZoneId = "Asia/Riyadh")
    {
        (dateFrom, dateTo) = ConvertToUtcRange(dateFrom, dateTo, timeZoneId);

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = !_currentUser.HasAnyAdminRole;

        var fuelTx = await _db.FuelTransactions
            .Where(f => f.DeletedAt == null && f.IsApproved
                && f.TransactionDate >= dateFrom.Value && f.TransactionDate <= dateTo.Value
                && (!subStationId.HasValue || f.SubStationId == subStationId.Value)
                && (!stationId.HasValue || f.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || f.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(f.SubStationId)))
            .Select(f => new {
                f.SubStationId,
                SubStationName = f.SubStation != null ? f.SubStation.Name : "?",
                TransactionDate = f.TransactionDate,
                f.QuantityLiters,
                TypeCode = f.TransactionTypeLookup != null ? f.TransactionTypeLookup.Code : null
            })
            .AsNoTracking().ToListAsync();

        var oilTx = await _db.OilTransactions
            .Where(o => o.DeletedAt == null && o.IsApproved
                && o.TransactionDate >= dateFrom.Value && o.TransactionDate <= dateTo.Value
                && (!subStationId.HasValue || o.SubStationId == subStationId.Value)
                && (!stationId.HasValue || o.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || o.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(o.SubStationId)))
            .Select(o => new {
                o.SubStationId,
                SubStationName = o.SubStation != null ? o.SubStation.Name : "?",
                TransactionDate = o.TransactionDate,
                o.QuantityLiters,
                TypeCode = o.TransactionTypeLookup != null ? o.TransactionTypeLookup.Code : null
            })
            .AsNoTracking().ToListAsync();

        var readings = await _db.DailyReadings
            .Where(r => r.DeletedAt == null
                && r.ReadingDate >= dateFrom.Value && r.ReadingDate <= dateTo.Value
                && (!subStationId.HasValue || r.Generator.SubStationId == subStationId.Value)
                && (!stationId.HasValue || r.Generator.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || r.Generator.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(r.Generator.SubStationId)))
            .Select(r => new {
                r.GeneratorId,
                SubStationName = r.Generator != null && r.Generator.SubStation != null ? r.Generator.SubStation.Name : "?",
                ReadingDate = r.ReadingDate,
                EnergyDiff = r.CurrentEnergyKWh - r.PrevEnergyKWh
            })
            .AsNoTracking().ToListAsync();

        var totalFuelConsumed = fuelTx.Where(f => f.TypeCode == "Consumption").Sum(f => f.QuantityLiters);
        var totalOilConsumed = oilTx.Where(o => o.TypeCode == "Consumption").Sum(o => o.QuantityLiters);
        var totalEnergy = readings.Sum(r => r.EnergyDiff);

        var activeStations = fuelTx.Select(f => f.SubStationId).Distinct().Count();
        var activeGens = readings.Select(r => r.GeneratorId).Distinct().Count();

        var summary = new FuelOilSummaryDto(totalFuelConsumed, totalOilConsumed, 0, 0, activeStations, activeGens);

        var consumptions = fuelTx
            .GroupBy(f => new { f.TransactionDate.Date, SubStationName = f.SubStationName })
            .Select(g =>
            {
                var date = g.Key.Date;
                var ssName = g.Key.SubStationName;
                var fuelLiters = g.Sum(f => f.QuantityLiters);
                var oilLiters = oilTx
                    .Where(o => o.SubStationName == ssName && o.TransactionDate.Date == date)
                    .Sum(o => o.QuantityLiters);
                var energyForDate = readings
                    .Where(r => r.SubStationName == ssName && r.ReadingDate.Date == date)
                    .Sum(r => r.EnergyDiff);
                var eff = fuelLiters > 0 ? energyForDate / fuelLiters * 0.3 : 0;
                return new FuelOilConsumptionDto(date, ssName, fuelLiters, oilLiters, Math.Round(eff, 1));
            })
            .OrderBy(c => c.Date).ToList();

        var trends = readings
            .GroupBy(r => r.ReadingDate.Date)
            .Select(g =>
            {
                var date = g.Key;
                var energy = g.Sum(r => r.EnergyDiff);
                var fuel = fuelTx.Where(f => f.TransactionDate.Date == date).Sum(f => f.QuantityLiters);
                var oil = oilTx.Where(o => o.TransactionDate.Date == date).Sum(o => o.QuantityLiters);
                return new FuelOilTrendDto(date, fuel, oil, energy);
            })
            .OrderBy(t => t.Date).ToList();

        var alertList = new List<FuelOilAlertDto>();
        var tanks = await _db.FuelTanks
            .Where(ft => ft.DeletedAt == null
                && (!subStationId.HasValue || ft.SubStationId == subStationId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(ft.SubStationId)))
            .Select(ft => new {
                ft.Name,
                SubStationName = ft.SubStation != null ? ft.SubStation.Name : "?",
                Stock = ft.ActualStockLiters ?? 0,
                MinAlert = ft.MinAlertLiters,
                DeadStock = ft.DeadStockLiters
            })
            .AsNoTracking().ToListAsync();

        foreach (var tank in tanks)
        {
            var stock = tank.Stock;
            if (tank.MinAlert.HasValue && stock <= tank.MinAlert.Value)
            {
                alertList.Add(new FuelOilAlertDto("Fuel", stock <= (tank.DeadStock ?? 0) ? "Critical" : "Warning",
                    $"Tank '{tank.Name}' at {tank.SubStationName}: {stock:F0}L remaining",
                    tank.SubStationName, null));
            }
        }

        return new FuelOilDashboardDto(summary, consumptions, trends, alertList);
    }

    public async Task<GeneratorsReportDto> GetGeneratorsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh")
    {
        (dateFrom, dateTo) = ConvertToUtcRange(dateFrom, dateTo, timeZoneId);

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = !_currentUser.HasAnyAdminRole;

        var gensQuery = _db.Generators
            .Include(g => g.SubStation).ThenInclude(ss => ss.Station).ThenInclude(s => s.Branch)
            .Include(g => g.StatusLookup)
            .Where(g => g.DeletedAt == null
                && (!subStationId.HasValue || g.SubStationId == subStationId.Value)
                && (!stationId.HasValue || g.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || g.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(g.SubStationId)))
            .AsNoTracking();

        var totalCount = await gensQuery.CountAsync();
        var gens = await gensQuery
            .OrderBy(g => g.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var genIds = gens.Select(g => g.Id).ToList();
        var readings = await _db.DailyReadings
            .Include(r => r.Generator)
            .Include(r => r.ReadingStatusLookup)
            .Where(r => r.DeletedAt == null && genIds.Contains(r.GeneratorId)
                && r.ReadingStatusLookup.Code == "Approved"
                && r.ReadingDate >= dateFrom.Value && r.ReadingDate <= dateTo.Value)
            .AsNoTracking().ToListAsync();

        var items = gens.Select(g =>
        {
            var genReadings = readings.Where(r => r.GeneratorId == g.Id).ToList();
            var energy = genReadings.Sum(r => r.CurrentEnergyKWh - r.PrevEnergyKWh);
            var fuel = genReadings.Sum(r => (double)r.StandardFuelRate * (r.CurrentEnergyKWh - r.PrevEnergyKWh));
            var hours = genReadings.Sum(r => r.CurrentRunningHours - r.PrevRunningHours);
            var eff = energy > 0 && fuel > 0 ? energy / (fuel / 0.3) * 100 : 0;
            var lastDate = genReadings.Any() ? genReadings.Max(r => r.ReadingDate) : (DateTime?)null;
            return new GeneratorReportItemDto(
                g.Id, g.Name, g.SerialNumber,
                g.SubStation?.Name ?? "?", g.SubStation?.Station?.Name ?? "?",
                g.SubStation?.Station?.Branch?.Name ?? "?",
                g.NominalPower, g.ActualPowerKW, (double)g.FuelConsumptionRatePerHour,
                Math.Round(energy, 1), Math.Round(fuel, 1), Math.Round(hours, 1),
                Math.Round(eff, 1), g.StatusLookup?.Name ?? "?", lastDate);
        }).ToList();

        var allActiveGens = await gensQuery.CountAsync();
        var allNominal = gens.Sum(g => g.NominalPower);
        var allActual = gens.Sum(g => (double)g.ActualPowerKW);
        var allEnergy = items.Sum(i => i.TotalEnergyProducedKWh);
        var allFuel = items.Sum(i => i.TotalFuelConsumedLiters);
        var avgEff = items.Any() ? items.Average(i => i.EfficiencyPercent) : 0;

        var summary = new GeneratorReportSummaryDto(
            totalCount, allActiveGens, totalCount - allActiveGens,
            allNominal, allActual, allEnergy, allFuel, Math.Round(avgEff, 1));

        return new GeneratorsReportDto(items, summary);
    }

    public async Task<StationReadingsReportDto> GetStationReadingsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh")
    {
        (dateFrom, dateTo) = ConvertToUtcRange(dateFrom, dateTo, timeZoneId);

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = !_currentUser.HasAnyAdminRole;

        var query = _db.SubStationReadings
            .Where(r => r.DeletedAt == null
                && r.ReadingStatusLookup.Code == "Approved"
                && r.ReadingDate >= dateFrom.Value && r.ReadingDate <= dateTo.Value
                && (!subStationId.HasValue || r.SubStationId == subStationId.Value)
                && (!stationId.HasValue || r.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || r.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(r.SubStationId)))
            .AsNoTracking();

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.ReadingDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new StationReadingReportItemDto(
                r.Id, r.ReadingDate, r.SubStation!.Name,
                r.SubStation.Station!.Name, r.SubStation.Station.Branch!.Name,
                r.Meter != null ? r.Meter.Name : "",
                r.PrevExportedEnergyKWh, r.CurrentExportedEnergyKWh,
                r.CurrentExportedEnergyKWh - r.PrevExportedEnergyKWh,
                r.ReadingStatusLookup!.Name))
            .ToListAsync();

        var summaryData = await query
            .Select(r => new {
                StatusCode = r.ReadingStatusLookup != null ? r.ReadingStatusLookup.Code : null,
                EnergyDiff = r.CurrentExportedEnergyKWh - r.PrevExportedEnergyKWh
            })
            .ToListAsync();

        var verified = summaryData.Count(r => r.StatusCode == "Verified" || r.StatusCode == "Approved");
        var pending = summaryData.Count(r => r.StatusCode == "Pending");
        var rejected = summaryData.Count(r => r.StatusCode == "Rejected");
        var totalEnergy = summaryData.Sum(r => r.EnergyDiff);

        var summary = new StationReadingReportSummaryDto(totalCount, verified, pending, rejected, totalEnergy);
        return new StationReadingsReportDto(items, summary);
    }

    public async Task<FuelTransactionsReportDto> GetFuelTransactionsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh",
        string? transactionTypeCode = null)
    {
        return await GetFuelTransactionsReportInternalAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize, timeZoneId, transactionTypeCode);
    }

    public async Task<OilTransactionsReportDto> GetOilTransactionsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh")
    {
        (dateFrom, dateTo) = ConvertToUtcRange(dateFrom, dateTo, timeZoneId);

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = !_currentUser.HasAnyAdminRole;

        var query = _db.OilTransactions
            .Where(o => o.DeletedAt == null
                && o.TransactionDate >= dateFrom.Value && o.TransactionDate <= dateTo.Value
                && (!subStationId.HasValue || o.SubStationId == subStationId.Value)
                && (!stationId.HasValue || o.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || o.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(o.SubStationId)))
            .AsNoTracking();

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OilTransactionReportItemDto(
                o.Id, o.TransactionDate, o.SubStation!.Name, o.SubStation.Station!.Name,
                o.OilLookup!.Name, o.QuantityLiters,
                o.TransactionTypeLookup!.Name,
                o.SupplierCompany,
                o.IsApproved ? "Approved" : "Draft", o.Notes))
            .ToListAsync();

        var allTxSummary = await query
            .Select(o => new {
                TypeCode = o.TransactionTypeLookup != null ? o.TransactionTypeLookup.Code : null,
                o.QuantityLiters
            })
            .ToListAsync();

        var incoming = allTxSummary.Where(o => o.TypeCode == "Purchase").ToList();
        var transfer = allTxSummary.Where(o => o.TypeCode == "TransferOut" || o.TypeCode == "Transfer").ToList();
        var consumption = allTxSummary.Where(o => o.TypeCode == "Consumption").ToList();

        var summary = new OilTransactionReportSummaryDto(
            totalCount,
            incoming.Sum(o => o.QuantityLiters),
            transfer.Sum(o => o.QuantityLiters),
            consumption.Sum(o => o.QuantityLiters),
            incoming.Count, transfer.Count, consumption.Count);

        return new OilTransactionsReportDto(items, summary);
    }

    public async Task<PeriodicInventoryReportDto> GetPeriodicInventoryReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh")
    {
        // Inventory is a business-day snapshot (normally stored at midnight), rather
        // than an event timestamp.  Use the station's current business date when no
        // end date is supplied so today's snapshot is not excluded while it is still
        // the previous UTC day.
        if (!dateTo.HasValue)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            dateTo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Date;
        }

        (dateFrom, dateTo) = ConvertToUtcRange(dateFrom, dateTo, timeZoneId);

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = !_currentUser.HasAnyAdminRole;

        var query = _db.PeriodicInventories
            .Include(p => p.SubStation).ThenInclude(ss => ss.Station)
            .Include(p => p.PeriodTypeLookup)
            .Include(p => p.FuelReadings).ThenInclude(fr => fr.FuelLookup)
            .Include(p => p.OilReadings).ThenInclude(or => or.OilLookup)
            .Where(p => p.DeletedAt == null
                && p.InventoryDate >= dateFrom.Value && p.InventoryDate <= dateTo.Value
                && (!subStationId.HasValue || p.SubStationId == subStationId.Value)
                && (!stationId.HasValue || p.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || p.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(p.SubStationId)))
            .AsNoTracking();

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.InventoryDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var mappedItems = items.Select(p =>
        {
            var fuelReceived = p.FuelReadings.Sum(fr => fr.ActualStockLiters);
            var oilReceived = p.OilReadings.Sum(or => or.ActualStockLiters);
            return new PeriodicInventoryReportItemDto(
                p.Id, p.Year, p.Month, p.PeriodTypeLookup?.Name ?? "?",
                p.SubStation?.Name ?? "?", p.SubStation?.Station?.Name ?? "?",
                p.InventoryDate, fuelReceived, 0, fuelReceived,
                oilReceived, 0, oilReceived,
                p.IsApproved ? "Approved" : "Draft");
        }).ToList();

        var allItemsSummary = await query
            .Select(p => new {
                p.IsApproved,
                FuelStock = p.FuelReadings.Sum(fr => (double?)fr.ActualStockLiters) ?? 0,
                OilStock = p.OilReadings.Sum(or => (double?)or.ActualStockLiters) ?? 0
            })
            .ToListAsync();

        var verified = allItemsSummary.Count(p => p.IsApproved);
        var pending = allItemsSummary.Count - verified;
        var totalFuelReceived = allItemsSummary.Sum(p => p.FuelStock);
        var totalOilReceived = allItemsSummary.Sum(p => p.OilStock);

        var summary = new PeriodicInventoryReportSummaryDto(
            totalCount, verified, pending,
            totalFuelReceived, 0, totalOilReceived, 0);

        return new PeriodicInventoryReportDto(mappedItems, summary);
    }

    public async Task<EfficiencyReportDto> GetEfficiencyReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh")
    {
        (dateFrom, dateTo) = ConvertToUtcRange(dateFrom, dateTo, timeZoneId);

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = !_currentUser.HasAnyAdminRole;

        var subsQuery = _db.SubStations
            .Include(ss => ss.Station).ThenInclude(s => s.Branch)
            .Where(ss => ss.DeletedAt == null
                && (!subStationId.HasValue || ss.Id == subStationId.Value)
                && (!stationId.HasValue || ss.StationId == stationId.Value)
                && (!branchId.HasValue || ss.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(ss.Id)))
            .AsNoTracking();

        var totalCount = await subsQuery.CountAsync();
        var subs = await subsQuery
            .OrderBy(ss => ss.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var subIds = subs.Select(ss => ss.Id).ToList();
        var readings = await _db.DailyReadings
            .Include(r => r.Generator)
            .Include(r => r.ReadingStatusLookup)
            .Where(r => r.DeletedAt == null && subIds.Contains(r.Generator.SubStationId)
                && r.ReadingStatusLookup.Code == "Approved"
                && r.ReadingDate >= dateFrom.Value && r.ReadingDate <= dateTo.Value)
            .AsNoTracking().ToListAsync();

        var stationReadings = await _db.SubStationReadings
            .Include(r => r.SubStation)
            .Include(r => r.ReadingStatusLookup)
            .Where(r => r.DeletedAt == null && subIds.Contains(r.SubStationId)
                && r.ReadingStatusLookup.Code == "Approved"
                && r.ReadingDate >= dateFrom.Value && r.ReadingDate <= dateTo.Value)
            .AsNoTracking().ToListAsync();

        var items = subs.Select(ss =>
        {
            var ssReadings = readings.Where(r => r.Generator.SubStationId == ss.Id).ToList();
            var energyProduced = ssReadings.Sum(r => r.CurrentEnergyKWh - r.PrevEnergyKWh);
            var fuelConsumed = ssReadings.Sum(r => (double)r.StandardFuelRate * (r.CurrentEnergyKWh - r.PrevEnergyKWh));
            var exported = stationReadings
                .Where(r => r.SubStationId == ss.Id)
                .Sum(r => r.CurrentExportedEnergyKWh - r.PrevExportedEnergyKWh);
            var eff = fuelConsumed > 0 ? energyProduced / (fuelConsumed / 0.3) * 100 : 0;
            var fuelPerKwh = energyProduced > 0 ? fuelConsumed / energyProduced : 0;
            var hours = ssReadings.Sum(r => r.CurrentRunningHours - r.PrevRunningHours);
            var loadFactor = hours > 0 && ss.CapacityKW.HasValue ? energyProduced / (hours * ss.CapacityKW.Value) * 100 : 0;
            return new EfficiencyReportItemDto(
                ss.Id, ss.Name, ss.Station?.Name ?? "?", ss.Station?.Branch?.Name ?? "?",
                energyProduced, exported, fuelConsumed,
                Math.Round(eff, 1), Math.Round(fuelPerKwh, 3), Math.Round(loadFactor, 1));
        }).ToList();

        var totalEnergy = items.Sum(i => i.TotalEnergyProducedKWh);
        var totalFuel = items.Sum(i => i.TotalFuelConsumedLiters);
        var avgEff = items.Any() ? items.Average(i => i.EfficiencyPercent) : 0;
        var mostEff = items.Any() ? items.OrderByDescending(i => i.EfficiencyPercent).First().SubStationName : "-";
        var leastEff = items.Any() ? items.OrderBy(i => i.EfficiencyPercent).First().SubStationName : "-";

        var summary = new EfficiencyReportSummaryDto(
            totalCount, totalEnergy, totalFuel,
            Math.Round(avgEff, 1), mostEff, leastEff);

        return new EfficiencyReportDto(items, summary);
    }

    public async Task<FuelTransactionsReportDto> GetFuelIncomingReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh")
    {
        var result = await GetFuelTransactionsReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize, timeZoneId, "Purchase");
        return result;
    }

    public async Task<FuelTransactionsReportDto> GetFuelTransferReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh")
    {
        var result = await GetFuelTransactionsReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize, timeZoneId, "TransferOut");
        return result;
    }

    public async Task<FuelTransactionsReportDto> GetFuelLossReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh")
    {
        var result = await GetFuelTransactionsReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize, timeZoneId, "Consumption");
        return result;
    }

    public async Task<InventorySummaryDto> GetInventorySummaryAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        string timeZoneId = "Asia/Riyadh")
    {
        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = !_currentUser.HasAnyAdminRole;

        var fuelTanks = await _db.FuelTanks
            .Where(ft => ft.DeletedAt == null
                && (!subStationId.HasValue || ft.SubStationId == subStationId.Value)
                && (!stationId.HasValue || ft.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || ft.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(ft.SubStationId)))
            .Select(ft => new {
                ActualStockLiters = ft.ActualStockLiters ?? 0,
                ft.MinAlertLiters
            })
            .AsNoTracking().ToListAsync();

        var oils = await _db.Oils
            .Where(o => o.DeletedAt == null
                && (!subStationId.HasValue || o.SubStationId == subStationId.Value)
                && (!stationId.HasValue || (o.SubStation != null && o.SubStation.StationId == stationId.Value))
                && (!branchId.HasValue || (o.SubStation != null && o.SubStation.Station.BranchId == branchId.Value))
                && (!hasScopeFilter || (o.SubStationId.HasValue && accessibleIds.Contains(o.SubStationId.Value))))
            .Select(o => new {
                CurrentStock = (double)o.CurrentStock,
                MinStock = (double)o.MinStock
            })
            .AsNoTracking().ToListAsync();

        var gens = await _db.Generators
            .AsNoTracking()
            .Where(g => g.DeletedAt == null
                && (!subStationId.HasValue || g.SubStationId == subStationId.Value)
                && (!stationId.HasValue || g.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || g.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(g.SubStationId)))
            .Select(g => g.StatusLookup != null ? g.StatusLookup.Code : null)
            .ToListAsync();

        var lowFuel = fuelTanks.Count(t => t.MinAlertLiters.HasValue && t.ActualStockLiters <= t.MinAlertLiters.Value);
        var lowOil = oils.Count(o => o.CurrentStock <= o.MinStock);

        return new InventorySummaryDto(
            fuelTanks.Count, oils.Count,
            fuelTanks.Sum(t => t.ActualStockLiters),
            oils.Sum(o => o.CurrentStock),
            lowFuel, lowOil,
            gens.Count(code => code == "Online" || code == "Active"),
            gens.Count);
    }

    private async Task<FuelTransactionsReportDto> GetFuelTransactionsReportInternalAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId,
        string? transactionTypeCode)
    {
        (dateFrom, dateTo) = ConvertToUtcRange(dateFrom, dateTo, timeZoneId);

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = !_currentUser.HasAnyAdminRole;

        var query = _db.FuelTransactions
            .Where(f => f.DeletedAt == null
                && f.TransactionDate >= dateFrom.Value && f.TransactionDate <= dateTo.Value
                && (!subStationId.HasValue || f.SubStationId == subStationId.Value)
                && (!stationId.HasValue || f.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || f.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(f.SubStationId) ||
                    (f.DestinationSubStationId.HasValue && accessibleIds.Contains(f.DestinationSubStationId.Value)))
                && (transactionTypeCode == null || f.TransactionTypeLookup!.Code == transactionTypeCode ||
                    (transactionTypeCode == "TransferOut" && f.TransactionTypeLookup!.Code == "Transfer")))
            .AsNoTracking();

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(f => f.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FuelTransactionReportItemDto(
                f.Id, f.TransactionDate, f.SubStation!.Name, f.SubStation.Station!.Name,
                f.FuelLookup!.Name, f.QuantityLiters,
                f.TransactionTypeLookup!.Name,
                f.DriverName, f.SupplierCompany, f.TruckNumber,
                f.SourceSubStation != null ? f.SourceSubStation.Name : null,
                f.DestinationSubStation != null ? f.DestinationSubStation.Name : null,
                f.IsApproved ? "Approved" : "Draft", f.Notes))
            .ToListAsync();

        var allTxSummary = await query
            .Select(f => new {
                TypeCode = f.TransactionTypeLookup != null ? f.TransactionTypeLookup.Code : null,
                f.QuantityLiters
            })
            .ToListAsync();

        var incoming = allTxSummary.Where(f => f.TypeCode == "Purchase").ToList();
        var transfer = allTxSummary.Where(f => f.TypeCode == "TransferOut" || f.TypeCode == "Transfer").ToList();
        var consumption = allTxSummary.Where(f => f.TypeCode == "Consumption").ToList();

        var summary = new FuelTransactionReportSummaryDto(
            totalCount,
            incoming.Sum(f => f.QuantityLiters),
            transfer.Sum(f => f.QuantityLiters),
            consumption.Sum(f => f.QuantityLiters),
            incoming.Count, transfer.Count, consumption.Count);

        return new FuelTransactionsReportDto(items, summary);
    }
}
