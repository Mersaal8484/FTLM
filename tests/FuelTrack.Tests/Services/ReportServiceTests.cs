using FuelTrack.Application.DTOs.Reports;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Tests.Services;

public class ReportServiceTests : IDisposable
{
    private readonly ReportService _service;
    private readonly FuelTrackDbContext _db;

    private readonly int _branchId = 1;
    private readonly int _stationId = 1;
    private readonly int _subStationId = 1;
    private readonly int _fuelLookupId = 1;
    private readonly int _oilLookupId = 100;

    public ReportServiceTests()
    {
        var o = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"ReportTestDb_{Guid.NewGuid()}")
            .Options;
        _db = new FuelTrackDbContext(o);

        _service = new ReportService(_db, new NullCacheService(), new StubHttpContextAccessor(), new StubUserScopeFilterService());

        SeedData();
    }

    private void SeedData()
    {
        // LookupCategories
        _db.LookupCategories.AddRange(
            new LookupCategory { Id = 1, Name = "FuelType" },
            new LookupCategory { Id = 2, Name = "TransactionType" },
            new LookupCategory { Id = 3, Name = "ReadingStatus" },
            new LookupCategory { Id = 4, Name = "PeriodType" },
            new LookupCategory { Id = 5, Name = "GeneratorStatus" });

        // Lookups
        _db.Lookups.AddRange(
            new Lookup { Id = _fuelLookupId, Name = "Diesel", CategoryId = 1, Code = "DIESEL" },
            new Lookup { Id = 100, Name = "Engine Oil", CategoryId = 1, Code = "OIL" },
            // TransactionTypes
            new Lookup { Id = 10, Name = "Purchase", CategoryId = 2, Code = "Purchase" },
            new Lookup { Id = 11, Name = "TransferOut", CategoryId = 2, Code = "TransferOut" },
            new Lookup { Id = 12, Name = "TransferIn", CategoryId = 2, Code = "TransferIn" },
            new Lookup { Id = 13, Name = "Consumption", CategoryId = 2, Code = "Consumption" },
            // ReadingStatus
            new Lookup { Id = 20, Name = "Approved", CategoryId = 3, Code = "Approved" },
            new Lookup { Id = 21, Name = "Pending", CategoryId = 3, Code = "Pending" },
            new Lookup { Id = 22, Name = "Rejected", CategoryId = 3, Code = "Rejected" },
            // PeriodType
            new Lookup { Id = 30, Name = "Monthly", CategoryId = 4, Code = "Monthly" },
            // GeneratorStatus
            new Lookup { Id = 40, Name = "Online", CategoryId = 5, Code = "Online" },
            new Lookup { Id = 41, Name = "Offline", CategoryId = 5, Code = "Offline" });

        // Hierarchy
        _db.Branches.Add(new Branch { Id = _branchId, Name = "Branch A", Code = "BA" });
        _db.Stations.Add(new Station { Id = _stationId, Name = "Station 1", Code = "S1", BranchId = _branchId });
        _db.SubStations.Add(new SubStation { Id = _subStationId, Name = "SubStation 1", Code = "SS1", StationId = _stationId });
        _db.SubStations.Add(new SubStation { Id = 2, Name = "SubStation 2", Code = "SS2", StationId = _stationId });

        // FuelTanks
        _db.FuelTanks.AddRange(
            new FuelTank { Id = 1, SubStationId = _subStationId, FuelLookupId = _fuelLookupId, Name = "Tank 1", Code = "T1", CapacityLiters = 5000, ActualStockLiters = 3000, MinAlertLiters = 1000, DeadStockLiters = 200 },
            new FuelTank { Id = 2, SubStationId = 2, FuelLookupId = _fuelLookupId, Name = "Tank 2", Code = "T2", CapacityLiters = 3000, ActualStockLiters = 150, MinAlertLiters = 500, DeadStockLiters = 100 });

        // Oils
        _db.Oils.AddRange(
            new Oil { Id = 1, OilLookupId = _oilLookupId, SubStationId = _subStationId, CurrentStock = 200, MinStock = 50 },
            new Oil { Id = 2, OilLookupId = _oilLookupId, SubStationId = 2, CurrentStock = 30, MinStock = 50 });

        // Meters
        _db.Meters.AddRange(
            new Meter { Id = 1, SubStationId = _subStationId, Name = "Meter 1", Code = "M-01" },
            new Meter { Id = 2, SubStationId = 2, Name = "Meter 2", Code = "M-02" });

        // Generators
        _db.Generators.AddRange(
            new Generator { Id = 1, SubStationId = _subStationId, Name = "Gen 1", SerialNumber = "SN-001", StatusLookupId = 40, NominalPower = 100, ActualPowerKW = 80, FuelConsumptionRatePerHour = 20m },
            new Generator { Id = 2, SubStationId = 2, Name = "Gen 2", SerialNumber = "SN-002", StatusLookupId = 41, NominalPower = 50, ActualPowerKW = 0, FuelConsumptionRatePerHour = 10m });

        // FuelTransactions
        _db.FuelTransactions.AddRange(
            new FuelTransaction { Id = 1, SubStationId = _subStationId, FuelLookupId = _fuelLookupId, TransactionTypeLookupId = 10, TransactionDate = DateTime.UtcNow.AddDays(-5), QuantityLiters = 1000, InvoiceNumber = "INV-001", SupplierCompany = "FuelCo", DriverName = "Ali", TruckNumber = "T-100", IsApproved = true },
            new FuelTransaction { Id = 2, SubStationId = _subStationId, FuelLookupId = _fuelLookupId, TransactionTypeLookupId = 11, TransactionDate = DateTime.UtcNow.AddDays(-3), QuantityLiters = 300, SourceSubStationId = _subStationId, DestinationSubStationId = 2, IsApproved = true },
            new FuelTransaction { Id = 3, SubStationId = _subStationId, FuelLookupId = _fuelLookupId, TransactionTypeLookupId = 13, TransactionDate = DateTime.UtcNow.AddDays(-1), QuantityLiters = 200, IsApproved = true, Notes = "Daily consumption" });

        // OilTransactions
        _db.OilTransactions.AddRange(
            new OilTransaction { Id = 1, SubStationId = _subStationId, OilLookupId = _oilLookupId, TransactionTypeLookupId = 10, TransactionDate = DateTime.UtcNow.AddDays(-4), QuantityLiters = 50, InvoiceNumber = "OIL-001", IsApproved = true },
            new OilTransaction { Id = 2, SubStationId = _subStationId, OilLookupId = _oilLookupId, TransactionTypeLookupId = 13, TransactionDate = DateTime.UtcNow.AddDays(-2), QuantityLiters = 10, IsApproved = true });

        // DailyReadings
        _db.DailyReadings.AddRange(
            new DailyReading { Id = 1, GeneratorId = 1, ReadingDate = DateTime.UtcNow.AddDays(-2), PrevEnergyKWh = 0, CurrentEnergyKWh = 100, PrevRunningHours = 0, CurrentRunningHours = 10, FuelLookupId = _fuelLookupId, StandardFuelRate = 0.25m, OilLookupId = _oilLookupId, OilUsedLiters = 2, ReadingStatusLookupId = 20 },
            new DailyReading { Id = 2, GeneratorId = 1, ReadingDate = DateTime.UtcNow.AddDays(-1), PrevEnergyKWh = 100, CurrentEnergyKWh = 250, PrevRunningHours = 10, CurrentRunningHours = 20, FuelLookupId = _fuelLookupId, StandardFuelRate = 0.25m, OilLookupId = _oilLookupId, OilUsedLiters = 3, ReadingStatusLookupId = 20 });

        // SubStationReadings
        _db.SubStationReadings.AddRange(
            new SubStationReading { Id = 1, SubStationId = _subStationId, MeterId = 1, ReadingDate = DateTime.UtcNow.AddDays(-2), PrevExportedEnergyKWh = 0, CurrentExportedEnergyKWh = 80, ReadingStatusLookupId = 20 },
            new SubStationReading { Id = 2, SubStationId = _subStationId, MeterId = 1, ReadingDate = DateTime.UtcNow.AddDays(-1), PrevExportedEnergyKWh = 80, CurrentExportedEnergyKWh = 200, ReadingStatusLookupId = 21 });

        // PeriodicInventories
        var inv = new PeriodicInventory { Id = 1, SubStationId = _subStationId, Year = DateTime.UtcNow.Year, Month = DateTime.UtcNow.Month, PeriodTypeLookupId = 30, InventoryDate = DateTime.Today, IsApproved = true };
        inv.FuelReadings.Add(new PeriodicInventoryFuel { Id = 1, PeriodicInventoryId = 1, FuelLookupId = _fuelLookupId, ActualStockLiters = 2800 });
        inv.OilReadings.Add(new PeriodicInventoryOil { Id = 1, PeriodicInventoryId = 1, OilLookupId = _oilLookupId, ActualStockLiters = 190 });
        _db.PeriodicInventories.Add(inv);

        _db.SaveChanges();
    }

    // ─── GetDashboardStatsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetDashboardStatsAsync_ReturnsCorrectSystemCounts()
    {
        var result = await _service.GetDashboardStatsAsync(null, null, null, null, null);

        Assert.Equal(1, result.System.TotalBranches);
        Assert.Equal(1, result.System.TotalStations);
        Assert.Equal(2, result.System.TotalSubStations);
        Assert.Equal(2, result.System.TotalGenerators);
        Assert.Equal(2, result.System.TotalFuelTanks);
        Assert.Equal(2, result.System.TotalOilTanks);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_ReturnsFuelSummary()
    {
        var result = await _service.GetDashboardStatsAsync(null, null, null, null, null);

        Assert.Equal(1000, result.Fuel.TotalReceivedLiters);
        Assert.Equal(300, result.Fuel.TotalTransferredLiters);
        Assert.Equal(200, result.Fuel.TotalConsumedLiters);
        Assert.Equal(2, result.Fuel.TankLevels.Count);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_ReturnsEnergySummary()
    {
        var result = await _service.GetDashboardStatsAsync(null, null, null, null, null);

        Assert.Equal(250, result.Energy.TotalProducedKWh);
        Assert.Equal(200, result.Energy.TotalExportedKWh);
        Assert.Equal(50, result.Energy.NetInternalKWh);
        Assert.Equal(2, result.Energy.ActiveGenerators);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_FiltersBySubStationId()
    {
        var result = await _service.GetDashboardStatsAsync(null, null, _subStationId, null, null);

        Assert.Equal(1, result.System.TotalSubStations);
        Assert.Equal(1, result.System.TotalGenerators);
        Assert.Equal(1, result.System.TotalFuelTanks);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_FiltersByDateRange()
    {
        var result = await _service.GetDashboardStatsAsync(null, null, null,
            DateTime.UtcNow.AddDays(-6), DateTime.UtcNow.AddDays(-4));

        Assert.Equal(1000, result.Fuel.TotalReceivedLiters);
        Assert.Equal(0, result.Fuel.TotalTransferredLiters);
        Assert.Equal(0, result.Fuel.TotalConsumedLiters);
    }

    // ─── GetFuelOilDashboardAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetFuelOilDashboardAsync_ReturnsSummary()
    {
        var result = await _service.GetFuelOilDashboardAsync(null, null, null, null, null);

        Assert.Equal(200, result.Summary.TotalFuelConsumedLiters);
        Assert.Equal(10, result.Summary.TotalOilConsumedLiters);
    }

    [Fact]
    public async Task GetFuelOilDashboardAsync_ReturnsConsumptions()
    {
        var result = await _service.GetFuelOilDashboardAsync(null, null, null, null, null);

        Assert.NotEmpty(result.Consumptions);
    }

    [Fact]
    public async Task GetFuelOilDashboardAsync_ReturnsAlertsForLowTanks()
    {
        var result = await _service.GetFuelOilDashboardAsync(null, null, null, null, null);

        Assert.NotEmpty(result.Alerts);
        Assert.Contains(result.Alerts, a => a.Type == "Fuel");
    }

    // ─── GetGeneratorsReportAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetGeneratorsReportAsync_ReturnsAllGenerators()
    {
        var result = await _service.GetGeneratorsReportAsync(null, null, null, null, null, 1, 20);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Summary.TotalGenerators);
    }

    [Fact]
    public async Task GetGeneratorsReportAsync_FiltersBySubStationId()
    {
        var result = await _service.GetGeneratorsReportAsync(null, null, _subStationId, null, null, 1, 20);

        Assert.Single(result.Items);
        Assert.Equal("Gen 1", result.Items[0].Name);
    }

    [Fact]
    public async Task GetGeneratorsReportAsync_IncludesReadingData()
    {
        var result = await _service.GetGeneratorsReportAsync(null, null, _subStationId, null, null, 1, 20);

        var gen = Assert.Single(result.Items);
        Assert.Equal(250, gen.TotalEnergyProducedKWh);
        Assert.True(gen.LastReadingDate.HasValue);
    }

    [Fact]
    public async Task GetGeneratorsReportAsync_PaginationWorks()
    {
        var page1 = await _service.GetGeneratorsReportAsync(null, null, null, null, null, 1, 1);
        var page2 = await _service.GetGeneratorsReportAsync(null, null, null, null, null, 2, 1);

        Assert.Single(page1.Items);
        Assert.Single(page2.Items);
        Assert.NotEqual(page1.Items[0].Id, page2.Items[0].Id);
    }

    // ─── GetStationReadingsReportAsync ────────────────────────────────────────

    [Fact]
    public async Task GetStationReadingsReportAsync_ReturnsReadings()
    {
        var result = await _service.GetStationReadingsReportAsync(null, null, null, null, null, 1, 20);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(200, result.Summary.TotalEnergyExportedKWh);
    }

    [Fact]
    public async Task GetStationReadingsReportAsync_FiltersBySubStationId()
    {
        var result = await _service.GetStationReadingsReportAsync(null, null, _subStationId, null, null, 1, 20);

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, i => Assert.Equal("SubStation 1", i.SubStationName));
    }

    [Fact]
    public async Task GetStationReadingsReportAsync_PaginationWorks()
    {
        var page1 = await _service.GetStationReadingsReportAsync(null, null, null, null, null, 1, 1);
        var page2 = await _service.GetStationReadingsReportAsync(null, null, null, null, null, 2, 1);

        Assert.Single(page1.Items);
        Assert.Single(page2.Items);
    }

    // ─── GetFuelTransactionsReportAsync ───────────────────────────────────────

    [Fact]
    public async Task GetFuelTransactionsReportAsync_ReturnsAllTransactions()
    {
        var result = await _service.GetFuelTransactionsReportAsync(null, null, null, null, null, 1, 20);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(1000, result.Summary.TotalIncomingLiters);
        Assert.Equal(300, result.Summary.TotalInternalTransferLiters);
        Assert.Equal(200, result.Summary.TotalConsumptionLiters);
    }

    [Fact]
    public async Task GetFuelTransactionsReportAsync_FiltersBySubStationId()
    {
        var result = await _service.GetFuelTransactionsReportAsync(null, null, _subStationId, null, null, 1, 20);

        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task GetFuelTransactionsReportAsync_FiltersByDateRange()
    {
        var result = await _service.GetFuelTransactionsReportAsync(null, null, null,
            DateTime.UtcNow.AddDays(-6), DateTime.UtcNow.AddDays(-4), 1, 20);

        Assert.Single(result.Items);
        Assert.Equal(1000, result.Items[0].QuantityLiters);
    }

    [Fact]
    public async Task GetFuelTransactionsReportAsync_PaginationWorks()
    {
        var page1 = await _service.GetFuelTransactionsReportAsync(null, null, null, null, null, 1, 2);
        var page2 = await _service.GetFuelTransactionsReportAsync(null, null, null, null, null, 2, 2);

        Assert.Equal(2, page1.Items.Count);
        Assert.Single(page2.Items);
        Assert.Equal(3, page1.Summary.TotalTransactions);
    }

    // ─── GetOilTransactionsReportAsync ────────────────────────────────────────

    [Fact]
    public async Task GetOilTransactionsReportAsync_ReturnsAllTransactions()
    {
        var result = await _service.GetOilTransactionsReportAsync(null, null, null, null, null, 1, 20);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(50, result.Summary.TotalIncomingLiters);
        Assert.Equal(10, result.Summary.TotalConsumptionLiters);
    }

    [Fact]
    public async Task GetOilTransactionsReportAsync_FiltersBySubStationId()
    {
        var result = await _service.GetOilTransactionsReportAsync(null, null, _subStationId, null, null, 1, 20);

        Assert.Equal(2, result.Items.Count);
    }

    // ─── GetPeriodicInventoryReportAsync ──────────────────────────────────────

    [Fact]
    public async Task GetPeriodicInventoryReportAsync_ReturnsInventoryData()
    {
        var result = await _service.GetPeriodicInventoryReportAsync(null, null, null, null, null, 1, 20);

        Assert.Single(result.Items);
        Assert.Equal(2800, result.Summary.TotalFuelReceivedLiters);
        Assert.Equal(190, result.Summary.TotalOilReceivedLiters);
    }

    [Fact]
    public async Task GetPeriodicInventoryReportAsync_FiltersBySubStationId()
    {
        var result = await _service.GetPeriodicInventoryReportAsync(null, null, _subStationId, null, null, 1, 20);

        Assert.Single(result.Items);
        Assert.Equal("SubStation 1", result.Items[0].SubStationName);
    }

    [Fact]
    public async Task GetPeriodicInventoryReportAsync_ReturnsEmptyForWrongDateRange()
    {
        var result = await _service.GetPeriodicInventoryReportAsync(null, null, null,
            DateTime.UtcNow.AddYears(-2), DateTime.UtcNow.AddYears(-1), 1, 20);

        Assert.Empty(result.Items);
    }

    // ─── GetEfficiencyReportAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetEfficiencyReportAsync_ReturnsAllSubStations()
    {
        var result = await _service.GetEfficiencyReportAsync(null, null, null, null, null, 1, 20);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Summary.TotalSubStations);
    }

    [Fact]
    public async Task GetEfficiencyReportAsync_FiltersBySubStationId()
    {
        var result = await _service.GetEfficiencyReportAsync(null, null, _subStationId, null, null, 1, 20);

        Assert.Single(result.Items);
        Assert.Equal("SubStation 1", result.Items[0].SubStationName);
    }

    [Fact]
    public async Task GetEfficiencyReportAsync_CalculatesEnergyProduced()
    {
        var result = await _service.GetEfficiencyReportAsync(null, null, _subStationId, null, null, 1, 20);

        var item = Assert.Single(result.Items);
        Assert.Equal(250, item.TotalEnergyProducedKWh);
        Assert.True(item.EfficiencyPercent > 0);
    }

    [Fact]
    public async Task GetEfficiencyReportAsync_PaginationWorks()
    {
        var page1 = await _service.GetEfficiencyReportAsync(null, null, null, null, null, 1, 1);
        var page2 = await _service.GetEfficiencyReportAsync(null, null, null, null, null, 2, 1);

        Assert.Single(page1.Items);
        Assert.Single(page2.Items);
        Assert.NotEqual(page1.Items[0].SubStationId, page2.Items[0].SubStationId);
    }

    // ─── GetDashboardStatsAsync - Edge Cases ──────────────────────────────────

    [Fact]
    public async Task GetDashboardStatsAsync_ReturnsEmptyWhenNoData()
    {
        // Fresh database with only hierarchy, no transactions or readings
        var freshDbOptions = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"ReportTestDb_Empty_{Guid.NewGuid()}")
            .Options;
        using var freshDb = new FuelTrackDbContext(freshDbOptions);
        var freshService = new ReportService(freshDb, new NullCacheService(), new StubHttpContextAccessor(), new StubUserScopeFilterService());

        var result = await freshService.GetDashboardStatsAsync(null, null, null, null, null);

        Assert.Equal(0, result.Fuel.TotalReceivedLiters);
        Assert.Equal(0, result.Energy.TotalProducedKWh);
        Assert.Equal(0, result.System.TotalBranches);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
