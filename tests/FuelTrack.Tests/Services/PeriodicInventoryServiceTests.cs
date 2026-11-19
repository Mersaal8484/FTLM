using FuelTrack.Application.DTOs.Efficiency;
using FuelTrack.Application.DTOs.Inventory;
using FuelTrack.Application.Interfaces;
using FuelTrack.Application.Services;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Tests.Services;

public class PeriodicInventoryServiceTests : IDisposable
{
    private readonly PeriodicInventoryService _service;
    private readonly FuelTrackDbContext _db;
    private readonly int _subStationId;

    public PeriodicInventoryServiceTests()
    {
        var o = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"InventoryTestDb_{Guid.NewGuid()}")
            .Options;
        _db = new FuelTrackDbContext(o);

        _service = new PeriodicInventoryService(_db, new StubUserScopeFilterService(), new StubCurrentUserService());

        _subStationId = 1;
        _db.SubStations.Add(new SubStation { Id = _subStationId, Name = "SS1", Code = "SS1" });
        _db.SaveChanges();
    }

    [Fact]
    public async Task StationInventory_SavesSeparateSiteReadingsAndRejectsInvalidBatch()
    {
        var first = await _db.SubStations.FindAsync(1);
        first!.StationId = 10;
        _db.SubStations.Add(new SubStation { Id = 2, StationId = 10, Name = "SS2", Code = "SS2" });
        _db.FuelTanks.AddRange(
            new FuelTank { Id = 1, SubStationId = 1, FuelLookupId = 3, CapacityLiters = 100 },
            new FuelTank { Id = 2, SubStationId = 2, FuelLookupId = 3, CapacityLiters = 200 });
        await _db.SaveChangesAsync();
        var request = new CreateStationInventoryDto(10, 2026, 11, 2, DateTime.UtcNow, "test",
            new() { new(1, 30), new(2, 40) }, new());
        var ids = await _service.CreateStationAsync(request, 1);
        Assert.Equal(2, ids.Count);
        var saved = await _db.PeriodicInventories.Include(i => i.FuelReadings).Where(i => ids.Contains(i.Id)).ToListAsync();
        Assert.Equal(30, saved.Single(i => i.SubStationId == 1).FuelReadings.Single().ActualStockLiters);
        Assert.Equal(40, saved.Single(i => i.SubStationId == 2).FuelReadings.Single().ActualStockLiters);
        var invalid = request with { FuelReadings = new() { new(1, 30), new(2, 201) } };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateStationAsync(invalid, 1));
        Assert.Equal(2, await _db.PeriodicInventories.CountAsync());
    }

    [Fact]
    public async Task InventoryLists_ReturnActualFuelAndOilTotals()
    {
        var category = new LookupCategory { Id = 2, Name = "PeriodType", DisplayName = "Period Type" };
        _db.LookupCategories.Add(category);
        _db.Lookups.Add(new Lookup { Id = 2, Name = "Monthly", CategoryId = 2 });
        var inventory = new PeriodicInventory
        {
            SubStationId = _subStationId, InventoryDate = DateTime.UtcNow, Year = 2026, Month = 11, PeriodTypeLookupId = 2,
            FuelReadings = new List<PeriodicInventoryFuel> { new() { FuelLookupId = 10, ActualStockLiters = 15000 }, new() { FuelLookupId = 11, ActualStockLiters = 500 } },
            OilReadings = new List<PeriodicInventoryOil> { new() { OilLookupId = 12, ActualStockLiters = 400 } }
        };
        _db.PeriodicInventories.Add(inventory);
        await _db.SaveChangesAsync();

        var all = Assert.Single(await _service.GetAllAsync());
        var site = Assert.Single(await _service.GetBySubStationAsync(_subStationId));
        Assert.Equal(15500, all.TotalFuelActualQty);
        Assert.Equal(400, all.TotalOilActualQty);
        Assert.Equal(all.TotalFuelActualQty, site.TotalFuelActualQty);
        Assert.Equal(all.TotalOilActualQty, site.TotalOilActualQty);
    }

    [Fact]
    public async Task CalculateEfficiency_ReturnsResult()
    {
        var lookupCategory = new LookupCategory { Id = 1, Name = "PeriodType", DisplayName = "Period Type" };
        _db.LookupCategories.Add(lookupCategory);
        var lookup = new Lookup { Id = 1, Name = "Monthly", CategoryId = 1 };
        _db.Lookups.Add(lookup);
        await _db.SaveChangesAsync();

        var inv = new PeriodicInventory
        {
            SubStationId = _subStationId,
            InventoryDate = DateTime.UtcNow,
            Year = 2026,
            Month = 1,
            PeriodTypeLookupId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.PeriodicInventories.Add(inv);
        await _db.SaveChangesAsync();

        var result = await _service.CalculateEfficiencyAsync(inv.Id);

        Assert.NotNull(result);
        Assert.Equal(0, result.ProducedEnergyKWh);
        Assert.Equal(0, result.ConsumedFuelLiters);
        Assert.Equal(0, result.EfficiencyPercent);
    }

    [Fact]
    public async Task CalculateEfficiency_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CalculateEfficiencyAsync(999));
    }

    [Fact]
    public async Task CreateAsync_CreatesInventory()
    {
        var lookupCategory = new LookupCategory { Id = 2, Name = "PeriodType", DisplayName = "Period Type" };
        _db.LookupCategories.Add(lookupCategory);
        var lookup = new Lookup { Id = 2, Name = "Monthly", CategoryId = 2 };
        _db.Lookups.Add(lookup);
        await _db.SaveChangesAsync();

        var dto = new CreatePeriodicInventoryDto(
            _subStationId, 2026, 6, 2, DateTime.UtcNow, "Test inventory", null, null);

        var result = await _service.CreateAsync(dto, 1);

        Assert.NotNull(result);
        Assert.Equal(_subStationId, result.SubStationId);
        Assert.Equal(2026, result.Year);
        Assert.Equal(6, result.Month);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
