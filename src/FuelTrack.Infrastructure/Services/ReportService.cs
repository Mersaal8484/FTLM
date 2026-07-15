using System.Security.Claims;
using FuelTrack.Application;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Reports;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly FuelTrackDbContext _db;
    private readonly ICacheService _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserScopeFilterService _scopeFilter;

    public ReportService(FuelTrackDbContext db, ICacheService cache, IHttpContextAccessor httpContextAccessor, IUserScopeFilterService scopeFilter)
    {
        _db = db;
        _cache = cache;
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

    private async Task<HashSet<int>> GetAccessibleSubStationIdsAsync()
    {
        if (IsAdmin()) return new HashSet<int>();
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return new HashSet<int>();
        var ids = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
        return new HashSet<int>(ids);
    }

    // ─── Dashboard Stats ──────────────────────────────────────────────────────

    public async Task<DashboardStatsResponseDto> GetDashboardStatsAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo)
    {
        var cacheKey = $"dashboard:stats:{branchId}:{stationId}:{subStationId}:{dateFrom:yyyyMMdd}:{dateTo:yyyyMMdd}";
        var cached = await _cache.GetAsync<DashboardStatsResponseDto>(cacheKey);
        if (cached is not null) return cached;

        dateFrom ??= DateTime.Today.AddDays(-30);
        dateTo ??= DateTime.Today;

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = accessibleIds.Count > 0;

        var fuelTanks = await _db.FuelTanks
            .Include(ft => ft.SubStation).ThenInclude(ss => ss.Station)
            .Include(ft => ft.FuelLookup)
            .Where(ft => ft.DeletedAt == null
                && (!subStationId.HasValue || ft.SubStationId == subStationId.Value)
                && (!stationId.HasValue || ft.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || ft.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(ft.SubStationId)))
            .AsNoTracking().ToListAsync();

        var fuelTx = await _db.FuelTransactions
            .Include(f => f.SubStation).ThenInclude(ss => ss.Station)
            .Include(f => f.TransactionTypeLookup)
            .Where(f => f.DeletedAt == null && f.IsApproved
                && f.TransactionDate >= dateFrom.Value && f.TransactionDate <= dateTo.Value
                && (!subStationId.HasValue || f.SubStationId == subStationId.Value)
                && (!stationId.HasValue || f.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || f.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(f.SubStationId)))
            .AsNoTracking().ToListAsync();

        var totalReceived = fuelTx.Where(f => f.TransactionTypeLookup?.Code == "Purchase").Sum(f => f.QuantityLiters);
        var totalTransferred = fuelTx.Where(f => f.TransactionTypeLookup?.Code == "TransferOut").Sum(f => f.QuantityLiters);
        var totalConsumed = fuelTx.Where(f => f.TransactionTypeLookup?.Code == "Consumption").Sum(f => f.QuantityLiters);

        var tankLevels = fuelTanks.Select(ft =>
        {
            var stock = ft.ActualStockLiters ?? 0;
            var pct = ft.CapacityLiters > 0 ? stock / ft.CapacityLiters * 100 : 0;
            var isLow = ft.MinAlertLiters.HasValue && stock <= ft.MinAlertLiters.Value;
            var isCritical = ft.DeadStockLiters.HasValue && stock <= ft.DeadStockLiters.Value;
            return new FuelTankLevelDto(
                ft.Id, ft.Name, ft.SubStation?.Name ?? "?",
                stock, ft.CapacityLiters, Math.Round(pct, 1), isLow, isCritical);
        }).ToList();

        var fuel = new FuelSummary(totalReceived, totalTransferred, totalConsumed,
            totalReceived - totalTransferred - totalConsumed, tankLevels);

        // Energy
        var readings = await _db.DailyReadings
            .Include(r => r.Generator).ThenInclude(g => g.SubStation).ThenInclude(ss => ss.Station)
            .Where(r => r.DeletedAt == null
                && r.ReadingDate >= dateFrom.Value && r.ReadingDate <= dateTo.Value
                && (!subStationId.HasValue || r.Generator.SubStationId == subStationId.Value)
                && (!stationId.HasValue || r.Generator.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || r.Generator.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(r.Generator.SubStationId)))
            .AsNoTracking().ToListAsync();

        var totalProduced = readings.Sum(r => r.CurrentEnergyKWh - r.PrevEnergyKWh);
        var totalHours = readings.Sum(r => r.CurrentRunningHours - r.PrevRunningHours);
        var avgLoad = totalHours > 0 ? totalProduced / totalHours : 0;
        var maxLoad = readings.Any() ? readings.Max(r => (r.CurrentEnergyKWh - r.PrevEnergyKWh) / Math.Max(r.CurrentRunningHours - r.PrevRunningHours, 0.01)) : 0;

        var stationReadings = await _db.SubStationReadings
            .Include(r => r.SubStation).ThenInclude(ss => ss.Station)
            .Where(r => r.DeletedAt == null
                && r.ReadingDate >= dateFrom.Value && r.ReadingDate <= dateTo.Value
                && (!subStationId.HasValue || r.SubStationId == subStationId.Value)
                && (!stationId.HasValue || r.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || r.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(r.SubStationId)))
            .AsNoTracking().ToListAsync();

        var totalExported = stationReadings.Sum(r => r.CurrentExportedEnergyKWh - r.PrevExportedEnergyKWh);
        var activeGens = await _db.Generators
            .Include(g => g.SubStation).ThenInclude(ss => ss.Station)
            .Where(g => g.DeletedAt == null
                && (!subStationId.HasValue || g.SubStationId == subStationId.Value)
                && (!stationId.HasValue || g.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || g.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(g.SubStationId)))
            .AsNoTracking().CountAsync();

        var energy = new EnergySummary(totalProduced, totalExported, totalProduced - totalExported,
            Math.Round(avgLoad, 1), Math.Round(maxLoad, 1), activeGens);

        // Oil
        var oils = await _db.Oils
            .Include(o => o.OilLookup).Include(o => o.SubStation).ThenInclude(ss => ss.Station)
            .Where(o => o.DeletedAt == null
                && (!subStationId.HasValue || o.SubStationId == subStationId.Value)
                && (!stationId.HasValue || (o.SubStation != null && o.SubStation.StationId == stationId.Value))
                && (!branchId.HasValue || (o.SubStation != null && o.SubStation.Station.BranchId == branchId.Value))
                && (!hasScopeFilter || (o.SubStationId.HasValue && accessibleIds.Contains(o.SubStationId.Value))))
            .AsNoTracking().ToListAsync();

        var oilTx = await _db.OilTransactions
            .Include(o => o.SubStation).ThenInclude(ss => ss.Station)
            .Include(o => o.TransactionTypeLookup)
            .Where(o => o.DeletedAt == null && o.IsApproved
                && o.TransactionDate >= dateFrom.Value && o.TransactionDate <= dateTo.Value
                && (!subStationId.HasValue || o.SubStationId == subStationId.Value)
                && (!stationId.HasValue || o.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || o.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(o.SubStationId)))
            .AsNoTracking().ToListAsync();

        var oilReceived = oilTx.Where(o => o.TransactionTypeLookup?.Code == "Purchase").Sum(o => o.QuantityLiters);
        var oilConsumed = oilTx.Where(o => o.TransactionTypeLookup?.Code == "Consumption").Sum(o => o.QuantityLiters);

        var oilTankLevels = oils.Select(o =>
        {
            var stock = (double)o.CurrentStock;
            var min = (double)o.MinStock;
            var pct = min > 0 ? stock / min * 100 : 100;
            return new OilTankLevelDto(o.Id, o.OilLookup?.Name ?? "?",
                o.SubStation?.Name ?? "-", stock, min, Math.Round(pct, 1));
        }).ToList();

        var oilSummary = new OilSummary(oilReceived, oilConsumed, oilTankLevels);

        // Alerts
        var lowFuel = tankLevels.Count(t => t.IsLow && !t.IsCritical);
        var criticalFuel = tankLevels.Count(t => t.IsCritical);
        var lowOil = oilTankLevels.Count(t => t.PercentageFull < 30);
        var staleThreshold = DateTime.Today.AddDays(-3);
        var staleReadings = await _db.DailyReadings
            .Where(r => r.DeletedAt == null && r.ReadingDate < staleThreshold
                && (!subStationId.HasValue || r.Generator.SubStationId == subStationId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(r.Generator.SubStationId)))
            .AsNoTracking().AnyAsync() ? 1 : 0;

        var alerts = new AlertSummary(lowFuel, criticalFuel, lowOil, staleReadings,
            lowFuel + criticalFuel + lowOil + staleReadings);

        // System
        var totalBranches = await _db.Branches.CountAsync(b => b.DeletedAt == null);
        var totalStations = await _db.Stations.CountAsync(s => s.DeletedAt == null
            && (!branchId.HasValue || s.BranchId == branchId.Value));
        var totalSubStations = await _db.SubStations.CountAsync(ss => ss.DeletedAt == null
            && (!subStationId.HasValue || ss.Id == subStationId.Value)
            && (!stationId.HasValue || ss.StationId == stationId.Value));
        var totalFuelTanks = fuelTanks.Count;
        var totalOilTanks = oils.Count;

        var allGens = await _db.Generators
            .Include(g => g.SubStation).ThenInclude(ss => ss.Station)
            .Include(g => g.StatusLookup)
            .Where(g => g.DeletedAt == null
                && (!subStationId.HasValue || g.SubStationId == subStationId.Value)
                && (!stationId.HasValue || g.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || g.SubStation.Station.BranchId == branchId.Value))
            .AsNoTracking().ToListAsync();

        var onlineGens = allGens.Count(g => g.StatusLookup?.Code == "Online" || g.StatusLookup?.Code == "Active");
        var offlineGens = allGens.Count(g => g.StatusLookup?.Code == "Offline" || g.StatusLookup?.Code == "Inactive");
        var maintenanceGens = allGens.Count(g => g.StatusLookup?.Code == "Maintenance");

        var system = new SystemSummary(totalBranches, totalStations, totalSubStations,
            allGens.Count, totalFuelTanks, totalOilTanks, onlineGens, offlineGens, maintenanceGens);

        var result = new DashboardStatsResponseDto(fuel, energy, oilSummary, alerts, system);
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));
        return result;
    }

    // ─── Fuel & Oil Dashboard ─────────────────────────────────────────────────

    public async Task<FuelOilDashboardDto> GetFuelOilDashboardAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo)
    {
        dateFrom ??= DateTime.Today.AddDays(-30);
        dateTo ??= DateTime.Today;

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = accessibleIds.Count > 0;

        var fuelTx = await _db.FuelTransactions
            .Include(f => f.SubStation).ThenInclude(ss => ss.Station)
            .Include(f => f.FuelLookup)
            .Include(f => f.TransactionTypeLookup)
            .Where(f => f.DeletedAt == null && f.IsApproved
                && f.TransactionDate >= dateFrom.Value && f.TransactionDate <= dateTo.Value
                && (!subStationId.HasValue || f.SubStationId == subStationId.Value)
                && (!stationId.HasValue || f.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || f.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(f.SubStationId)))
            .AsNoTracking().ToListAsync();

        var oilTx = await _db.OilTransactions
            .Include(o => o.SubStation).ThenInclude(ss => ss.Station)
            .Include(o => o.OilLookup)
            .Include(o => o.TransactionTypeLookup)
            .Where(o => o.DeletedAt == null && o.IsApproved
                && o.TransactionDate >= dateFrom.Value && o.TransactionDate <= dateTo.Value
                && (!subStationId.HasValue || o.SubStationId == subStationId.Value)
                && (!stationId.HasValue || o.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || o.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(o.SubStationId)))
            .AsNoTracking().ToListAsync();

        var readings = await _db.DailyReadings
            .Include(r => r.Generator).ThenInclude(g => g.SubStation).ThenInclude(ss => ss.Station)
            .Where(r => r.DeletedAt == null
                && r.ReadingDate >= dateFrom.Value && r.ReadingDate <= dateTo.Value
                && (!subStationId.HasValue || r.Generator.SubStationId == subStationId.Value)
                && (!stationId.HasValue || r.Generator.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || r.Generator.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(r.Generator.SubStationId)))
            .AsNoTracking().ToListAsync();

        var totalFuelConsumed = fuelTx.Where(f => f.TransactionTypeLookup?.Code == "Consumption").Sum(f => f.QuantityLiters);
        var totalOilConsumed = oilTx.Where(o => o.TransactionTypeLookup?.Code == "Consumption").Sum(o => o.QuantityLiters);
        var totalEnergy = readings.Sum(r => r.CurrentEnergyKWh - r.PrevEnergyKWh);

        var activeStations = fuelTx.Select(f => f.SubStationId).Distinct().Count();
        var activeGens = readings.Select(r => r.GeneratorId).Distinct().Count();

        var summary = new FuelOilSummaryDto(totalFuelConsumed, totalOilConsumed, 0, 0, activeStations, activeGens);

        var consumptions = fuelTx
            .GroupBy(f => new { f.TransactionDate.Date, SubStationName = f.SubStation?.Name ?? "?" })
            .Select(g =>
            {
                var date = g.Key.Date;
                var ssName = g.Key.SubStationName;
                var fuelLiters = g.Sum(f => f.QuantityLiters);
                var oilLiters = oilTx
                    .Where(o => o.SubStation?.Name == ssName && o.TransactionDate.Date == date)
                    .Sum(o => o.QuantityLiters);
                var energyForDate = readings
                    .Where(r => r.Generator.SubStation?.Name == ssName && r.ReadingDate.Date == date)
                    .Sum(r => r.CurrentEnergyKWh - r.PrevEnergyKWh);
                var eff = fuelLiters > 0 ? energyForDate / fuelLiters * 0.3 : 0;
                return new FuelOilConsumptionDto(date, ssName, fuelLiters, oilLiters, Math.Round(eff, 1));
            })
            .OrderBy(c => c.Date).ToList();

        var trends = readings
            .GroupBy(r => r.ReadingDate.Date)
            .Select(g =>
            {
                var date = g.Key;
                var energy = g.Sum(r => r.CurrentEnergyKWh - r.PrevEnergyKWh);
                var fuel = fuelTx.Where(f => f.TransactionDate.Date == date).Sum(f => f.QuantityLiters);
                var oil = oilTx.Where(o => o.TransactionDate.Date == date).Sum(o => o.QuantityLiters);
                return new FuelOilTrendDto(date, fuel, oil, energy);
            })
            .OrderBy(t => t.Date).ToList();

        var alertList = new List<FuelOilAlertDto>();
        var tanks = await _db.FuelTanks
            .Include(ft => ft.SubStation)
            .Where(ft => ft.DeletedAt == null
                && (!subStationId.HasValue || ft.SubStationId == subStationId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(ft.SubStationId)))
            .AsNoTracking().ToListAsync();

        foreach (var tank in tanks)
        {
            var stock = tank.ActualStockLiters ?? 0;
            if (tank.MinAlertLiters.HasValue && stock <= tank.MinAlertLiters.Value)
            {
                alertList.Add(new FuelOilAlertDto("Fuel", stock <= (tank.DeadStockLiters ?? 0) ? "Critical" : "Warning",
                    $"Tank '{tank.Name}' at {tank.SubStation?.Name}: {stock:F0}L remaining",
                    tank.SubStation?.Name ?? "?", null));
            }
        }

        return new FuelOilDashboardDto(summary, consumptions, trends, alertList);
    }

    // ─── Generators Report ────────────────────────────────────────────────────

    public async Task<GeneratorsReportDto> GetGeneratorsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize)
    {
        dateFrom ??= DateTime.Today.AddDays(-30);
        dateTo ??= DateTime.Today;

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = accessibleIds.Count > 0;

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
            .Where(r => r.DeletedAt == null && genIds.Contains(r.GeneratorId)
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

    // ─── Station Readings Report ──────────────────────────────────────────────

    public async Task<StationReadingsReportDto> GetStationReadingsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize)
    {
        dateFrom ??= DateTime.Today.AddDays(-30);
        dateTo ??= DateTime.Today;

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = accessibleIds.Count > 0;

        var query = _db.SubStationReadings
            .Include(r => r.SubStation).ThenInclude(ss => ss.Station).ThenInclude(s => s.Branch)
            .Include(r => r.Meter)
            .Include(r => r.ReadingStatusLookup)
            .Where(r => r.DeletedAt == null
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
                r.Meter!.Code,
                r.PrevExportedEnergyKWh, r.CurrentExportedEnergyKWh,
                r.CurrentExportedEnergyKWh - r.PrevExportedEnergyKWh,
                r.ReadingStatusLookup!.Name))
            .ToListAsync();

        var allReadings = await query.ToListAsync();
        var verified = allReadings.Count(r => r.ReadingStatusLookup?.Code == "Verified" || r.ReadingStatusLookup?.Code == "Approved");
        var pending = allReadings.Count(r => r.ReadingStatusLookup?.Code == "Pending");
        var rejected = allReadings.Count(r => r.ReadingStatusLookup?.Code == "Rejected");
        var totalEnergy = allReadings.Sum(r => r.CurrentExportedEnergyKWh - r.PrevExportedEnergyKWh);

        var summary = new StationReadingReportSummaryDto(totalCount, verified, pending, rejected, totalEnergy);
        return new StationReadingsReportDto(items, summary);
    }

    // ─── Fuel Transactions Report ─────────────────────────────────────────────

    public async Task<FuelTransactionsReportDto> GetFuelTransactionsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize)
    {
        dateFrom ??= DateTime.Today.AddDays(-30);
        dateTo ??= DateTime.Today;

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = accessibleIds.Count > 0;

        var query = _db.FuelTransactions
            .Include(f => f.SubStation).ThenInclude(ss => ss.Station)
            .Include(f => f.FuelLookup)
            .Include(f => f.TransactionTypeLookup)
            .Include(f => f.SourceSubStation)
            .Include(f => f.DestinationSubStation)
            .Where(f => f.DeletedAt == null
                && f.TransactionDate >= dateFrom.Value && f.TransactionDate <= dateTo.Value
                && (!subStationId.HasValue || f.SubStationId == subStationId.Value)
                && (!stationId.HasValue || f.SubStation.StationId == stationId.Value)
                && (!branchId.HasValue || f.SubStation.Station.BranchId == branchId.Value)
                && (!hasScopeFilter || accessibleIds.Contains(f.SubStationId)))
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

        var allTx = await query.ToListAsync();
        var incoming = allTx.Where(f => f.TransactionTypeLookup?.Code == "Purchase");
        var transfer = allTx.Where(f => f.TransactionTypeLookup?.Code == "TransferOut");
        var consumption = allTx.Where(f => f.TransactionTypeLookup?.Code == "Consumption");

        var summary = new FuelTransactionReportSummaryDto(
            totalCount,
            incoming.Sum(f => f.QuantityLiters),
            transfer.Sum(f => f.QuantityLiters),
            consumption.Sum(f => f.QuantityLiters),
            incoming.Count(), transfer.Count(), consumption.Count());

        return new FuelTransactionsReportDto(items, summary);
    }

    // ─── Oil Transactions Report ──────────────────────────────────────────────

    public async Task<OilTransactionsReportDto> GetOilTransactionsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize)
    {
        dateFrom ??= DateTime.Today.AddDays(-30);
        dateTo ??= DateTime.Today;

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = accessibleIds.Count > 0;

        var query = _db.OilTransactions
            .Include(o => o.SubStation).ThenInclude(ss => ss.Station)
            .Include(o => o.OilLookup)
            .Include(o => o.TransactionTypeLookup)
            .Include(o => o.SourceSubStation)
            .Include(o => o.DestinationSubStation)
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
                o.SupplierCompany, o.DriverName,
                o.SourceSubStation != null ? o.SourceSubStation.Name : null,
                o.DestinationSubStation != null ? o.DestinationSubStation.Name : null,
                o.IsApproved ? "Approved" : "Draft", o.Notes))
            .ToListAsync();

        var allTx = await query.ToListAsync();
        var incoming = allTx.Where(o => o.TransactionTypeLookup?.Code == "Purchase");
        var transfer = allTx.Where(o => o.TransactionTypeLookup?.Code == "TransferOut");
        var consumption = allTx.Where(o => o.TransactionTypeLookup?.Code == "Consumption");

        var summary = new OilTransactionReportSummaryDto(
            totalCount,
            incoming.Sum(o => o.QuantityLiters),
            transfer.Sum(o => o.QuantityLiters),
            consumption.Sum(o => o.QuantityLiters),
            incoming.Count(), transfer.Count(), consumption.Count());

        return new OilTransactionsReportDto(items, summary);
    }

    // ─── Periodic Inventory Report ────────────────────────────────────────────

    public async Task<PeriodicInventoryReportDto> GetPeriodicInventoryReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize)
    {
        dateFrom ??= DateTime.Today.AddMonths(-6);
        dateTo ??= DateTime.Today;

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = accessibleIds.Count > 0;

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

        var allItems = await query.ToListAsync();
        var verified = allItems.Count(p => p.IsApproved);
        var pending = allItems.Count - verified;
        var totalFuelReceived = allItems.Sum(p => p.FuelReadings.Sum(fr => fr.ActualStockLiters));
        var totalOilReceived = allItems.Sum(p => p.OilReadings.Sum(or => or.ActualStockLiters));

        var summary = new PeriodicInventoryReportSummaryDto(
            totalCount, verified, pending,
            totalFuelReceived, 0, totalOilReceived, 0);

        return new PeriodicInventoryReportDto(mappedItems, summary);
    }

    // ─── Efficiency Report ────────────────────────────────────────────────────

    public async Task<EfficiencyReportDto> GetEfficiencyReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize)
    {
        dateFrom ??= DateTime.Today.AddDays(-30);
        dateTo ??= DateTime.Today;

        var accessibleIds = await GetAccessibleSubStationIdsAsync();
        bool hasScopeFilter = accessibleIds.Count > 0;

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
            .Where(r => r.DeletedAt == null && subIds.Contains(r.Generator.SubStationId)
                && r.ReadingDate >= dateFrom.Value && r.ReadingDate <= dateTo.Value)
            .AsNoTracking().ToListAsync();

        var stationReadings = await _db.SubStationReadings
            .Include(r => r.SubStation)
            .Where(r => r.DeletedAt == null && subIds.Contains(r.SubStationId)
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
}
