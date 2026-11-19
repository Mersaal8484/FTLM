using FuelTrack.Application.DTOs.Inventory;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FuelTrack.Tests.Services;

public sealed class PeriodicInventoryServiceCommercialTests : IDisposable
{
    private readonly FuelTrackDbContext _db;

    public PeriodicInventoryServiceCommercialTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"PeriodicInventoryCommercial_{Guid.NewGuid()}").Options);

        var periodTypeCat = new LookupCategory { Id = 10, Name = "PeriodType", DisplayName = "Period Type", IsActive = true };
        var fuelCat = new LookupCategory { Id = 20, Name = "FuelType", DisplayName = "Fuel Type", IsActive = true };
        var oilCat = new LookupCategory { Id = 30, Name = "OilType", DisplayName = "Oil Type", IsActive = true };

        _db.AddRange(
            periodTypeCat, fuelCat, oilCat,
            new Lookup { Id = 100, CategoryId = 10, Category = periodTypeCat, Code = "Monthly", Name = "شهري", IsActive = true },
            new Lookup { Id = 200, CategoryId = 20, Category = fuelCat, Code = "Diesel", Name = "ديزل", IsActive = true },
            new Lookup { Id = 300, CategoryId = 30, Category = oilCat, Code = "15W40", Name = "زيت 15W-40", IsActive = true },
            new Branch { Id = 1, Name = "Technical Branch", Code = "TECH-PS01", IsActive = true },
            new Station { Id = 1, BranchId = 1, Name = "Commercial Station", Code = "PS01", IsActive = true },
            // Single active technical substation scope for commercial mode
            new SubStation { Id = 1, StationId = 1, Name = "Commercial Station", Code = "PS01", IsActive = true },
            new FuelTank { Id = 1, SubStationId = 1, Name = "Tank 1", FuelLookupId = 200, CapacityLiters = 50000, IsActive = true },
            new User { Id = 1, UserName = "admin@ft.com", Email = "admin@ft.com", FullName = "Admin User", IsActive = true }
        );
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void EnableCommercialMode()
    {
        var existing = _db.CustomerFeatureSettings.FirstOrDefault(f => f.Key == CommercialStationModeService.FeatureKey);
        if (existing != null)
        {
            existing.IsEnabled = true;
            existing.Value = CommercialStationModeService.CommercialModeValue;
        }
        else
        {
            _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
            {
                Key = CommercialStationModeService.FeatureKey,
                IsEnabled = true,
                Value = CommercialStationModeService.CommercialModeValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_CommercialMode_ResolvesTechnicalScopeAndStoresSubStationId()
    {
        EnableCommercialMode();
        var commercialMode = new CommercialStationModeService(_db);
        var service = new PeriodicInventoryService(_db, new StubUserScopeFilterService(), new StubCurrentUserService(), commercialMode);

        var dto = new CreatePeriodicInventoryDto(
            SubStationId: 0, // Ignored by commercial mode
            Year: 2026,
            Month: 11,
            PeriodTypeLookupId: 100,
            InventoryDate: DateTime.UtcNow,
            Notes: "Commercial stocktake draft",
            FuelReadings: new List<FuelReadingDto> { new(200, 15000) },
            OilReadings: new List<OilReadingDto>()
        );

        var result = await service.CreateAsync(dto, 1);

        Assert.NotNull(result);
        Assert.Equal(1, result.SubStationId);
        Assert.Equal("Commercial Station", result.SubStationName);
    }

    [Fact]
    public async Task CreateAsync_EnterpriseMode_RequiresSubStationId()
    {
        var commercialMode = new CommercialStationModeService(_db);
        var service = new PeriodicInventoryService(_db, new StubUserScopeFilterService(), new StubCurrentUserService(), commercialMode);

        var dto = new CreatePeriodicInventoryDto(
            SubStationId: 0,
            Year: 2026,
            Month: 11,
            PeriodTypeLookupId: 100,
            InventoryDate: DateTime.UtcNow,
            Notes: "Enterprise test without site",
            FuelReadings: null,
            OilReadings: null
        );

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto, 1));
    }

    [Fact]
    public async Task GetAllAsync_CommercialMode_FiltersByTechnicalSubStation()
    {
        EnableCommercialMode();
        var commercialMode = new CommercialStationModeService(_db);
        var service = new PeriodicInventoryService(_db, new StubUserScopeFilterService(), new StubCurrentUserService(), commercialMode);

        // Create draft for site 1
        await service.CreateAsync(new CreatePeriodicInventoryDto(
            SubStationId: 0,
            Year: 2026,
            Month: 11,
            PeriodTypeLookupId: 100,
            InventoryDate: DateTime.UtcNow,
            Notes: "Site 1 inventory",
            FuelReadings: null,
            OilReadings: null
        ), 1);

        // Add second SubStation and an inventory for it directly to DB
        _db.SubStations.Add(new SubStation { Id = 2, StationId = 1, Name = "Other Site", Code = "OTH", IsActive = false });
        _db.PeriodicInventories.Add(new PeriodicInventory
        {
            SubStationId = 2,
            Year = 2026,
            Month = 11,
            PeriodTypeLookupId = 100,
            InventoryDate = DateTime.UtcNow,
            Notes = "Site 2 inventory",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();

        var list = await service.GetAllAsync();

        Assert.Single(list);
        Assert.Equal(1, list[0].SubStationId);
    }

    [Fact]
    public async Task GetAllAsync_CommercialMode_UnresolvableScope_DoesNotExposeInventories()
    {
        EnableCommercialMode();
        _db.SubStations.Add(new SubStation { Id = 2, StationId = 1, Name = "Other", Code = "OTH", IsActive = true });
        _db.PeriodicInventories.Add(new PeriodicInventory { Id = 123, SubStationId = 1, PeriodTypeLookupId = 100, Year = 2026, Month = 11, InventoryDate = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        var service = new PeriodicInventoryService(_db, new StubUserScopeFilterService(), new StubCurrentUserService(), new CommercialStationModeService(_db));
        Assert.Empty(await service.GetAllAsync());
        Assert.Null(await service.GetByIdAsync(123));
        Assert.Empty(await service.GetBySubStationAsync(1));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ApproveAsync(123, 1));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync(123));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CalculateEfficiencyAsync(123));
        var inventory = await _db.PeriodicInventories.FindAsync(123);
        Assert.False(inventory!.IsApproved);
        Assert.Null(inventory.DeletedAt);
    }

    [Fact]
    public async Task UpdateAsync_CannotChangeApprovalStatus()
    {
        _db.PeriodicInventories.Add(new PeriodicInventory { Id = 456, SubStationId = 1, PeriodTypeLookupId = 100, Year = 2026, Month = 11, InventoryDate = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        var service = new PeriodicInventoryService(_db, new StubUserScopeFilterService(), new StubCurrentUserService(), new CommercialStationModeService(_db));
        var dto = new CreatePeriodicInventoryDto(1, 2026, 11, 100, DateTime.UtcNow, null, null, null, true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(456, dto, 1));
        Assert.False((await _db.PeriodicInventories.FindAsync(456))!.IsApproved);
    }

    [Fact]
    public async Task CreateAsync_CommercialMode_UnresolvableScope_ThrowsInvalidOperationException()
    {
        EnableCommercialMode();
        // Deactivate all substations so resolver returns null (Count != 1)
        var subs = _db.SubStations.ToList();
        foreach (var s in subs) { s.IsActive = false; s.DeletedAt = DateTime.UtcNow; }
        _db.SaveChanges();

        var commercialMode = new CommercialStationModeService(_db);
        var service = new PeriodicInventoryService(_db, new StubUserScopeFilterService(), new StubCurrentUserService(), commercialMode);

        var dto = new CreatePeriodicInventoryDto(
            SubStationId: 0,
            Year: 2026,
            Month: 11,
            PeriodTypeLookupId: 100,
            InventoryDate: DateTime.UtcNow,
            Notes: "Unresolvable test",
            FuelReadings: null,
            OilReadings: null
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto, 1));
        Assert.Contains("Commercial mode has no active technical station scope", ex.Message);
    }
}
