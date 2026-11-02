using FuelTrack.Application.DTOs.FuelMovements;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FuelTrack.Tests.Services;

public class StockSummaryServiceTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly StockSummaryService _service;
    private readonly Mock<IUserScopeFilterService> _scopeFilterMock;
    private readonly StubCurrentUserService _currentUser;

    public StockSummaryServiceTests()
    {
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"StockSummaryTestDb_{Guid.NewGuid()}")
            .Options;
        _db = new FuelTrackDbContext(options);

        _scopeFilterMock = new Mock<IUserScopeFilterService>();
        _currentUser = new StubCurrentUserService { IsAdmin = true, UserId = 1 };
        _service = new StockSummaryService(_db, _scopeFilterMock.Object, _currentUser);
    }

    [Fact]
    public async Task GetFuelStockSummaryAsync_WithInventoryOnFirstDay_UsesItAsOpeningBalance()
    {
        var branch = new Branch { Name = "Main", Code = "MB" };
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync();

        var station = new Station { BranchId = branch.Id, Name = "S", Code = "S" };
        _db.Stations.Add(station);
        await _db.SaveChangesAsync();

        var subStation = new SubStation { Name = "SS1", Code = "SS1", StationId = station.Id };
        _db.SubStations.Add(subStation);
        await _db.SaveChangesAsync();

        var fuelLookup = new LookupCategory { Name = "FuelType", DisplayName = "FuelType" };
        _db.LookupCategories.Add(fuelLookup);
        await _db.SaveChangesAsync();
        var fuelType = new Lookup { CategoryId = fuelLookup.Id, Code = "Diesel", Name = "Diesel" };
        _db.Lookups.Add(fuelType);
        await _db.SaveChangesAsync();

        _db.FuelTanks.Add(new FuelTank
        {
            SubStationId = subStation.Id,
            Name = "T1",
            FuelLookupId = fuelType.Id,
            CapacityLiters = 1000,
            ActualStockLiters = 500
        });
        await _db.SaveChangesAsync();

        var periodTypeCategory = new LookupCategory { Name = "PeriodType", DisplayName = "PeriodType" };
        _db.LookupCategories.Add(periodTypeCategory);
        await _db.SaveChangesAsync();
        var periodType = new Lookup { CategoryId = periodTypeCategory.Id, Code = "Monthly", Name = "Monthly" };
        _db.Lookups.Add(periodType);
        await _db.SaveChangesAsync();

        var inventory = new PeriodicInventory
        {
            SubStationId = subStation.Id,
            Year = 2026,
            Month = 8,
            PeriodTypeLookupId = periodType.Id,
            InventoryDate = new DateTime(2026, 8, 1),
            IsApproved = true
        };
        _db.PeriodicInventories.Add(inventory);
        await _db.SaveChangesAsync();

        _db.PeriodicInventoryFuels.Add(new PeriodicInventoryFuel
        {
            PeriodicInventoryId = inventory.Id,
            FuelLookupId = fuelType.Id,
            ActualStockLiters = 800
        });
        await _db.SaveChangesAsync();

        var fromDate = new DateTime(2026, 8, 1);
        var toDate = new DateTime(2026, 8, 31);
        var result = await _service.GetFuelStockSummaryAsync(fromDate, toDate, 1);

        var row = result.Single();
        Assert.Equal(800, row.BaseStock);
    }

    [Fact]
    public async Task GetFuelStockSummaryAsync_WithInPeriodInventory_ExcludesEarlierMovements()
    {
        var branch = new Branch { Name = "Main", Code = "MB" };
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync();

        var station = new Station { BranchId = branch.Id, Name = "S", Code = "S" };
        _db.Stations.Add(station);
        await _db.SaveChangesAsync();

        var subStation = new SubStation { Name = "SS1", Code = "SS1", StationId = station.Id };
        _db.SubStations.Add(subStation);
        await _db.SaveChangesAsync();

        var fuelLookup = new LookupCategory { Name = "FuelType", DisplayName = "FuelType" };
        _db.LookupCategories.Add(fuelLookup);
        await _db.SaveChangesAsync();
        var fuelType = new Lookup { CategoryId = fuelLookup.Id, Code = "Diesel", Name = "Diesel" };
        _db.Lookups.Add(fuelType);
        await _db.SaveChangesAsync();

        var transactionTypeCategory = new LookupCategory { Name = "TransactionType", DisplayName = "TransactionType" };
        _db.LookupCategories.Add(transactionTypeCategory);
        await _db.SaveChangesAsync();
        var purchaseType = new Lookup { CategoryId = transactionTypeCategory.Id, Code = "Purchase", Name = "Purchase" };
        _db.Lookups.Add(purchaseType);
        await _db.SaveChangesAsync();

        _db.FuelTanks.Add(new FuelTank
        {
            SubStationId = subStation.Id,
            Name = "T1",
            FuelLookupId = fuelType.Id,
            CapacityLiters = 1000,
            ActualStockLiters = 500
        });
        await _db.SaveChangesAsync();

        var periodTypeCategory = new LookupCategory { Name = "PeriodType", DisplayName = "PeriodType" };
        _db.LookupCategories.Add(periodTypeCategory);
        await _db.SaveChangesAsync();
        var periodType = new Lookup { CategoryId = periodTypeCategory.Id, Code = "FirstHalf", Name = "First Half" };
        _db.Lookups.Add(periodType);
        await _db.SaveChangesAsync();

        var inventory = new PeriodicInventory
        {
            SubStationId = subStation.Id,
            Year = 2026,
            Month = 8,
            PeriodTypeLookupId = periodType.Id,
            InventoryDate = new DateTime(2026, 8, 15),
            IsApproved = true
        };
        _db.PeriodicInventories.Add(inventory);
        await _db.SaveChangesAsync();

        _db.PeriodicInventoryFuels.Add(new PeriodicInventoryFuel
        {
            PeriodicInventoryId = inventory.Id,
            FuelLookupId = fuelType.Id,
            ActualStockLiters = 800
        });
        await _db.SaveChangesAsync();

        _db.FuelTransactions.Add(new FuelTransaction
        {
            SubStationId = subStation.Id,
            FuelLookupId = fuelType.Id,
            TransactionTypeLookupId = purchaseType.Id,
            TransactionDate = new DateTime(2026, 8, 10),
            QuantityLiters = 200,
            IsApproved = true
        });
        _db.FuelTransactions.Add(new FuelTransaction
        {
            SubStationId = subStation.Id,
            FuelLookupId = fuelType.Id,
            TransactionTypeLookupId = purchaseType.Id,
            TransactionDate = new DateTime(2026, 8, 20),
            QuantityLiters = 100,
            IsApproved = true
        });
        await _db.SaveChangesAsync();

        var fromDate = new DateTime(2026, 8, 1);
        var toDate = new DateTime(2026, 8, 31);
        var result = await _service.GetFuelStockSummaryAsync(fromDate, toDate, 1);

        var row = result.Single();
        Assert.Equal(800, row.BaseStock);
        Assert.Equal(100, row.Inbound);
    }

    [Fact]
    public async Task GetFuelStockSummaryAsync_WithPrePeriodInventory_UsesItAsOpeningBalance()
    {
        var branch = new Branch { Name = "Main", Code = "MB" };
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync();

        var station = new Station { BranchId = branch.Id, Name = "S", Code = "S" };
        _db.Stations.Add(station);
        await _db.SaveChangesAsync();

        var subStation = new SubStation { Name = "SS1", Code = "SS1", StationId = station.Id };
        _db.SubStations.Add(subStation);
        await _db.SaveChangesAsync();

        var fuelLookup = new LookupCategory { Name = "FuelType", DisplayName = "FuelType" };
        _db.LookupCategories.Add(fuelLookup);
        await _db.SaveChangesAsync();
        var fuelType = new Lookup { CategoryId = fuelLookup.Id, Code = "Diesel", Name = "Diesel" };
        _db.Lookups.Add(fuelType);
        await _db.SaveChangesAsync();

        var transactionTypeCategory = new LookupCategory { Name = "TransactionType", DisplayName = "TransactionType" };
        _db.LookupCategories.Add(transactionTypeCategory);
        await _db.SaveChangesAsync();
        var purchaseType = new Lookup { CategoryId = transactionTypeCategory.Id, Code = "Purchase", Name = "Purchase" };
        _db.Lookups.Add(purchaseType);
        await _db.SaveChangesAsync();

        _db.FuelTanks.Add(new FuelTank
        {
            SubStationId = subStation.Id,
            Name = "T1",
            FuelLookupId = fuelType.Id,
            CapacityLiters = 1000,
            ActualStockLiters = 500
        });
        await _db.SaveChangesAsync();

        var periodTypeCategory = new LookupCategory { Name = "PeriodType", DisplayName = "PeriodType" };
        _db.LookupCategories.Add(periodTypeCategory);
        await _db.SaveChangesAsync();
        var periodType = new Lookup { CategoryId = periodTypeCategory.Id, Code = "Monthly", Name = "Monthly" };
        _db.Lookups.Add(periodType);
        await _db.SaveChangesAsync();

        var inventory = new PeriodicInventory
        {
            SubStationId = subStation.Id,
            Year = 2026,
            Month = 7,
            PeriodTypeLookupId = periodType.Id,
            InventoryDate = new DateTime(2026, 7, 31),
            IsApproved = true
        };
        _db.PeriodicInventories.Add(inventory);
        await _db.SaveChangesAsync();

        _db.PeriodicInventoryFuels.Add(new PeriodicInventoryFuel
        {
            PeriodicInventoryId = inventory.Id,
            FuelLookupId = fuelType.Id,
            ActualStockLiters = 700
        });
        await _db.SaveChangesAsync();

        _db.FuelTransactions.Add(new FuelTransaction
        {
            SubStationId = subStation.Id,
            FuelLookupId = fuelType.Id,
            TransactionTypeLookupId = purchaseType.Id,
            TransactionDate = new DateTime(2026, 8, 5),
            QuantityLiters = 100,
            IsApproved = true
        });
        await _db.SaveChangesAsync();

        var fromDate = new DateTime(2026, 8, 1);
        var toDate = new DateTime(2026, 8, 31);
        var result = await _service.GetFuelStockSummaryAsync(fromDate, toDate, 1);

        var row = result.Single();
        Assert.Equal(700, row.BaseStock);
        Assert.Equal(100, row.Inbound);
    }

    [Fact]
    public async Task GetFuelStockSummaryAsync_WithNoInventory_UsesTankStock()
    {
        var branch = new Branch { Name = "Main", Code = "MB" };
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync();

        var station = new Station { BranchId = branch.Id, Name = "S", Code = "S" };
        _db.Stations.Add(station);
        await _db.SaveChangesAsync();

        var subStation = new SubStation { Name = "SS1", Code = "SS1", StationId = station.Id };
        _db.SubStations.Add(subStation);
        await _db.SaveChangesAsync();

        var fuelLookup = new LookupCategory { Name = "FuelType", DisplayName = "FuelType" };
        _db.LookupCategories.Add(fuelLookup);
        await _db.SaveChangesAsync();
        var fuelType = new Lookup { CategoryId = fuelLookup.Id, Code = "Diesel", Name = "Diesel" };
        _db.Lookups.Add(fuelType);
        await _db.SaveChangesAsync();

        _db.FuelTanks.Add(new FuelTank
        {
            SubStationId = subStation.Id,
            Name = "T1",
            FuelLookupId = fuelType.Id,
            CapacityLiters = 1000,
            ActualStockLiters = 500
        });
        await _db.SaveChangesAsync();

        var fromDate = new DateTime(2026, 8, 1);
        var toDate = new DateTime(2026, 8, 31);
        var result = await _service.GetFuelStockSummaryAsync(fromDate, toDate, 1);

        var row = result.Single();
        Assert.Equal(500, row.BaseStock);
    }

    [Fact]
    public async Task GetOilStockSummaryAsync_WithPrePeriodInventory_UsesItAsOpeningBalance()
    {
        var branch = new Branch { Name = "Main", Code = "MB" };
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync();

        var station = new Station { BranchId = branch.Id, Name = "S", Code = "S" };
        _db.Stations.Add(station);
        await _db.SaveChangesAsync();

        var subStation = new SubStation { Name = "SS1", Code = "SS1", StationId = station.Id };
        _db.SubStations.Add(subStation);
        await _db.SaveChangesAsync();

        var oilLookup = new LookupCategory { Name = "OilType", DisplayName = "OilType" };
        _db.LookupCategories.Add(oilLookup);
        await _db.SaveChangesAsync();
        var oilType = new Lookup { CategoryId = oilLookup.Id, Code = "SAE40", Name = "SAE 40" };
        _db.Lookups.Add(oilType);
        await _db.SaveChangesAsync();

        var transactionTypeCategory = new LookupCategory { Name = "TransactionType", DisplayName = "TransactionType" };
        _db.LookupCategories.Add(transactionTypeCategory);
        await _db.SaveChangesAsync();
        var purchaseType = new Lookup { CategoryId = transactionTypeCategory.Id, Code = "Purchase", Name = "Purchase" };
        _db.Lookups.Add(purchaseType);
        await _db.SaveChangesAsync();

        _db.Oils.Add(new Oil
        {
            SubStationId = subStation.Id,
            OilLookupId = oilType.Id,
            CurrentStock = 300
        });
        await _db.SaveChangesAsync();

        var periodTypeCategory = new LookupCategory { Name = "PeriodType", DisplayName = "PeriodType" };
        _db.LookupCategories.Add(periodTypeCategory);
        await _db.SaveChangesAsync();
        var periodType = new Lookup { CategoryId = periodTypeCategory.Id, Code = "Monthly", Name = "Monthly" };
        _db.Lookups.Add(periodType);
        await _db.SaveChangesAsync();

        var inventory = new PeriodicInventory
        {
            SubStationId = subStation.Id,
            Year = 2026,
            Month = 7,
            PeriodTypeLookupId = periodType.Id,
            InventoryDate = new DateTime(2026, 7, 31),
            IsApproved = true
        };
        _db.PeriodicInventories.Add(inventory);
        await _db.SaveChangesAsync();

        _db.PeriodicInventoryOils.Add(new PeriodicInventoryOil
        {
            PeriodicInventoryId = inventory.Id,
            OilLookupId = oilType.Id,
            ActualStockLiters = 600
        });
        await _db.SaveChangesAsync();

        _db.OilTransactions.Add(new OilTransaction
        {
            SubStationId = subStation.Id,
            OilLookupId = oilType.Id,
            TransactionTypeLookupId = purchaseType.Id,
            TransactionDate = new DateTime(2026, 8, 5),
            QuantityLiters = 50,
            IsApproved = true
        });
        await _db.SaveChangesAsync();

        var fromDate = new DateTime(2026, 8, 1);
        var toDate = new DateTime(2026, 8, 31);
        var result = await _service.GetOilStockSummaryAsync(fromDate, toDate, 1);

        var row = result.Single();
        Assert.Equal(600, row.BaseStock);
        Assert.Equal(50, row.Inbound);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
