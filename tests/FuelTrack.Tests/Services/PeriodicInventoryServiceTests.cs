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
