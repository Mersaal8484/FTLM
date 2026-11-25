using FuelTrack.Application.DTOs.FuelTanks;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FuelTrack.Tests.Services;

public sealed class FuelTankServiceDailyTankTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly CommercialStationModeService _commercialMode;
    private readonly StubCurrentUserService _currentUser;
    private readonly StubUserScopeFilterService _scopeFilter;
    private readonly FuelTankService _tankService;

    public FuelTankServiceDailyTankTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"DailyTankTests_{Guid.NewGuid()}").Options);

        _commercialMode = new CommercialStationModeService(_db);
        _currentUser = new StubCurrentUserService { UserId = 1, IsAdmin = true };
        _scopeFilter = new StubUserScopeFilterService();
        _tankService = new FuelTankService(_db, _scopeFilter, _currentUser, _commercialMode);

        SeedData();
    }

    private void SeedData()
    {
        var branch = new Branch { Id = 1, Code = "BR-01", Name = "Commercial Branch", IsActive = true };
        var station = new Station { Id = 1, BranchId = 1, Code = "ST-01", Name = "Commercial Station", IsActive = true };
        var subStation1 = new SubStation { Id = 1, StationId = 1, Code = "SITE-01", Name = "Site 1", IsActive = true };
        // Keep exactly one active technical scope for commercial mode. This inactive
        // site still supplies cross-site validation fixtures without invalidating it.
        var subStation2 = new SubStation { Id = 2, StationId = 1, Code = "SITE-02", Name = "Site 2", IsActive = false };

        var fuelCategory = new LookupCategory { Id = 1, Name = "FuelType", DisplayName = "Fuel types", IsActive = true };
        var fuelLookup = new Lookup { Id = 1, CategoryId = fuelCategory.Id, Code = "DIESEL", Name = "Diesel", IsActive = true };

        var gen1 = new Generator { Id = 1, SubStationId = 1, Name = "Gen-01", SerialNumber = "G01", IsActive = true };
        var genSameSite = new Generator { Id = 4, SubStationId = 1, Name = "Gen-04", SerialNumber = "G04", IsActive = true };
        var gen2 = new Generator { Id = 2, SubStationId = 2, Name = "Gen-02", SerialNumber = "G02", IsActive = true };
        var genDeleted = new Generator { Id = 3, SubStationId = 1, Name = "Gen-Del", SerialNumber = "G03", IsActive = false, DeletedAt = DateTime.UtcNow };

        _db.Branches.Add(branch);
        _db.Stations.Add(station);
        _db.SubStations.AddRange(subStation1, subStation2);
        _db.LookupCategories.Add(fuelCategory);
        _db.Lookups.Add(fuelLookup);
        _db.Generators.AddRange(gen1, genSameSite, gen2, genDeleted);
        _db.SaveChanges();
    }

    private async Task EnableCommercialModeAsync()
    {
        var setting = await _db.CustomerFeatureSettings.FirstOrDefaultAsync(x => x.Key == "commercial_station_mode");
        if (setting == null)
        {
            _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
            {
                Key = "commercial_station_mode",
                IsEnabled = true,
                Value = "commercial"
            });
        }
        else
        {
            setting.IsEnabled = true;
            setting.Value = "commercial";
        }
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_WithValidGenerator_InCommercialMode_ResolvesTechnicalSiteAndCreatesTank()
    {
        await EnableCommercialModeAsync();

        var dto = new CreateFuelTankDto(
            SubStationId: 0,
            FuelLookupId: 1,
            Name: "Daily Tank G1",
            Code: "DT-01",
            CapacityLiters: 1000,
            TankRole: TankRole.Daily,
            GeneratorId: 1);

        var result = await _tankService.CreateAsync(dto, 1);

        Assert.NotNull(result);
        Assert.Equal(1, result.SubStationId);
        Assert.Equal(TankRole.Daily, result.TankRole);
        Assert.Equal(1, result.GeneratorId);
    }

    [Fact]
    public async Task CreateAsync_WithGeneratorInAnotherSite_ThrowsArgumentException()
    {
        await EnableCommercialModeAsync();

        var dto = new CreateFuelTankDto(
            SubStationId: 0,
            FuelLookupId: 1,
            Name: "Daily Tank Foreign Gen",
            Code: "DT-02",
            CapacityLiters: 1000,
            TankRole: TankRole.Daily,
            GeneratorId: 2); // Gen 2 belongs to SubStation 2, commercial scope resolves to SubStation 1

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tankService.CreateAsync(dto, 1));
        Assert.Contains("does not belong to substation", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithDeletedOrNonExistentGenerator_ThrowsKeyNotFoundException()
    {
        await EnableCommercialModeAsync();

        // Non-existent generator
        var dto1 = new CreateFuelTankDto(
            SubStationId: 0,
            FuelLookupId: 1,
            Name: "Daily Tank Missing Gen",
            Code: "DT-99",
            CapacityLiters: 1000,
            TankRole: TankRole.Daily,
            GeneratorId: 999);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _tankService.CreateAsync(dto1, 1));

        // Soft-deleted generator
        var dto2 = new CreateFuelTankDto(
            SubStationId: 0,
            FuelLookupId: 1,
            Name: "Daily Tank Deleted Gen",
            Code: "DT-Del",
            CapacityLiters: 1000,
            TankRole: TankRole.Daily,
            GeneratorId: 3);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _tankService.CreateAsync(dto2, 1));
    }

    [Fact]
    public async Task CreateAsync_DailyWithoutGenerator_Succeeds()
    {
        await EnableCommercialModeAsync();

        var dto = new CreateFuelTankDto(
            SubStationId: 0,
            FuelLookupId: 1,
            Name: "Daily Tank No Gen",
            Code: "DT-NoGen",
            CapacityLiters: 1000,
            TankRole: TankRole.Daily,
            GeneratorId: null);

        var result = await _tankService.CreateAsync(dto, 1);
        Assert.NotNull(result);
        Assert.Null(result.GeneratorId);
        Assert.Equal(TankRole.Daily, result.TankRole);
    }

    [Fact]
    public async Task CreateAsync_UnlinkedDailyTank_NextToExistingMainTank_Succeeds()
    {
        await EnableCommercialModeAsync();

        var mainDto = new CreateFuelTankDto(
            SubStationId: 0,
            FuelLookupId: 1,
            Name: "Main Tank",
            Code: "MT-01",
            CapacityLiters: 5000,
            TankRole: TankRole.Main);
        var mainTank = await _tankService.CreateAsync(mainDto, 1);
        Assert.NotNull(mainTank);

        var dailyDto = new CreateFuelTankDto(
            SubStationId: 0,
            FuelLookupId: 1,
            Name: "Daily Tank Unlinked",
            Code: "DT-Unlinked",
            CapacityLiters: 1000,
            TankRole: TankRole.Daily,
            GeneratorId: null);
        var dailyTank = await _tankService.CreateAsync(dailyDto, 1);
        Assert.NotNull(dailyTank);
        Assert.Equal(TankRole.Daily, dailyTank.TankRole);
        Assert.Null(dailyTank.GeneratorId);
    }

    [Fact]
    public async Task CreateAsync_SecondDailyTankForSameGenerator_ThrowsInvalidOperationException()
    {
        await EnableCommercialModeAsync();

        var dto1 = new CreateFuelTankDto(
            SubStationId: 0,
            FuelLookupId: 1,
            Name: "Daily Tank 1",
            Code: "DT-01",
            CapacityLiters: 1000,
            TankRole: TankRole.Daily,
            GeneratorId: 1);
        await _tankService.CreateAsync(dto1, 1);

        var dto2 = new CreateFuelTankDto(
            SubStationId: 0,
            FuelLookupId: 1,
            Name: "Daily Tank 2",
            Code: "DT-02",
            CapacityLiters: 1000,
            TankRole: TankRole.Daily,
            GeneratorId: 1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _tankService.CreateAsync(dto2, 1));
        Assert.Contains("already has a daily tank", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_DailyWithoutGenerator_Succeeds()
    {
        await EnableCommercialModeAsync();

        var dto1 = new CreateFuelTankDto(
            SubStationId: 0,
            FuelLookupId: 1,
            Name: "Daily Tank 1",
            Code: "DT-01",
            CapacityLiters: 1000,
            TankRole: TankRole.Daily,
            GeneratorId: 1);
        var created = await _tankService.CreateAsync(dto1, 1);

        var updateDto = new UpdateFuelTankDto(
            SubStationId: null,
            FuelLookupId: null,
            Name: "Updated Tank",
            Code: null,
            CapacityLiters: null,
            MinAlertLiters: null,
            DeadStockLiters: null,
            ActualStockLiters: null,
            HasMeter: null,
            MeterReading: null,
            MeterReadingActual: null,
            Notes: null,
            IsActive: true,
            TankRole: TankRole.Daily,
            GeneratorId: null);

        var updated = await _tankService.UpdateAsync(created.Id, updateDto, 1);
        Assert.NotNull(updated);
        Assert.Equal("Updated Tank", updated.Name);
    }

    [Fact]
    public async Task UpdateAsync_ValidUpdate_KeepsGenerator()
    {
        await EnableCommercialModeAsync();

        var dto1 = new CreateFuelTankDto(
            SubStationId: 0,
            FuelLookupId: 1,
            Name: "Daily Tank 1",
            Code: "DT-01",
            CapacityLiters: 1000,
            TankRole: TankRole.Daily,
            GeneratorId: 1);
        var created = await _tankService.CreateAsync(dto1, 1);

        var updateDto = new UpdateFuelTankDto(
            SubStationId: null,
            FuelLookupId: null,
            Name: "Renamed Tank",
            Code: null,
            CapacityLiters: 1200,
            MinAlertLiters: null,
            DeadStockLiters: null,
            ActualStockLiters: null,
            HasMeter: null,
            MeterReading: null,
            MeterReadingActual: null,
            Notes: null,
            IsActive: true,
            TankRole: null, // unchanged
            GeneratorId: null); // omitted in update, must be preserved

        var updated = await _tankService.UpdateAsync(created.Id, updateDto, 1);
        Assert.Equal("Renamed Tank", updated.Name);
        Assert.Equal(1, updated.GeneratorId);
        Assert.Equal(TankRole.Daily, updated.TankRole);
    }

    [Fact]
    public async Task ParentTankRules_ValidateSubStationAndSelfReference()
    {
        await EnableCommercialModeAsync();

        // Main tank in site 1
        var mainDto = new CreateFuelTankDto(
            SubStationId: 0,
            FuelLookupId: 1,
            Name: "Main Storage Tank",
            Code: "MT-01",
            CapacityLiters: 50000,
            TankRole: TankRole.Main);
        var mainTank = await _tankService.CreateAsync(mainDto, 1);

        // Daily tank linking to main tank in site 1 -> OK
        var dailyDto = new CreateFuelTankDto(
            SubStationId: 0,
            FuelLookupId: 1,
            Name: "Daily Tank with Parent",
            Code: "DT-Parent",
            CapacityLiters: 1000,
            TankRole: TankRole.Daily,
            GeneratorId: 1,
            ParentTankId: mainTank.Id);
        var dailyTank = await _tankService.CreateAsync(dailyDto, 1);
        Assert.Equal(mainTank.Id, dailyTank.ParentTankId);

        // Daily tank with non-existent parent tank -> KeyNotFoundException
        var badParentDto = new CreateFuelTankDto(
            SubStationId: 0,
            FuelLookupId: 1,
            Name: "Daily Bad Parent",
            Code: "DT-BadP",
            CapacityLiters: 1000,
            ParentTankId: 9999);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _tankService.CreateAsync(badParentDto, 1));

        // Update tank to be its own parent -> ArgumentException
        var selfParentUpdate = new UpdateFuelTankDto(
            SubStationId: null,
            FuelLookupId: null,
            Name: null,
            Code: null,
            CapacityLiters: null,
            MinAlertLiters: null,
            DeadStockLiters: null,
            ActualStockLiters: null,
            HasMeter: null,
            MeterReading: null,
            MeterReadingActual: null,
            Notes: null,
            IsActive: null,
            ParentTankId: dailyTank.Id);
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tankService.UpdateAsync(dailyTank.Id, selfParentUpdate, 1));
        Assert.Contains("cannot be its own parent", ex.Message);
    }

    [Fact]
    public async Task EnterprisePath_Unchanged_UsesExplicitSubStation()
    {
        // Commercial mode disabled
        var dto = new CreateFuelTankDto(
            SubStationId: 2,
            FuelLookupId: 1,
            Name: "Enterprise Tank Site 2",
            Code: "ENT-02",
            CapacityLiters: 20000,
            TankRole: null);

        var result = await _tankService.CreateAsync(dto, 1);
        Assert.Equal(2, result.SubStationId);
    }

    [Fact]
    public async Task CreateAsync_CommercialNullRole_DefaultsToMain()
    {
        await EnableCommercialModeAsync();

        var result = await _tankService.CreateAsync(new CreateFuelTankDto(
            SubStationId: 0, FuelLookupId: 1, Name: "Default main", Code: "MT-DEFAULT", CapacityLiters: 5000), 1);

        Assert.Equal(TankRole.Main, result.TankRole);
        Assert.Null(result.GeneratorId);
    }

    [Fact]
    public async Task CreateAsync_ActiveDuplicateMainFuelType_ThrowsButSoftDeletedTankDoesNotBlock()
    {
        await EnableCommercialModeAsync();
        var first = await _tankService.CreateAsync(new CreateFuelTankDto(
            0, 1, "Main 1", "MT-01", 5000, TankRole: TankRole.Main), 1);

        var duplicate = new CreateFuelTankDto(0, 1, "Main 2", "MT-02", 5000, TankRole: TankRole.Main);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _tankService.CreateAsync(duplicate, 1));
        Assert.Contains("main tank for this fuel type", ex.Message);

        var entity = await _db.FuelTanks.FindAsync(first.Id);
        entity!.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var replacement = await _tankService.CreateAsync(duplicate, 1);
        Assert.Equal(TankRole.Main, replacement.TankRole);
    }

    [Fact]
    public async Task CreateAsync_DailyTanksForDifferentGeneratorsMayUseSameFuelType()
    {
        await EnableCommercialModeAsync();

        var first = await _tankService.CreateAsync(new CreateFuelTankDto(
            0, 1, "Daily G1", "DT-01", 1000, TankRole: TankRole.Daily, GeneratorId: 1), 1);
        var second = await _tankService.CreateAsync(new CreateFuelTankDto(
            0, 1, "Daily G4", "DT-04", 1000, TankRole: TankRole.Daily, GeneratorId: 4), 1);

        Assert.Equal(1, first.GeneratorId);
        Assert.Equal(4, second.GeneratorId);
    }

    [Fact]
    public async Task UpdateAsync_CommercialGenericUpdateCannotChangeTankRole()
    {
        await EnableCommercialModeAsync();
        var main = await _tankService.CreateAsync(new CreateFuelTankDto(
            0, 1, "Main", "MT-01", 5000, TankRole: TankRole.Main), 1);

        var update = new UpdateFuelTankDto(null, null, null, null, null, null, null, null, null, null, null, null, null,
            TankRole: TankRole.Daily, GeneratorId: 1);
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tankService.UpdateAsync(main.Id, update, 1));

        Assert.Contains("role cannot be changed", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_CommercialLegacyMainWithoutGenerator_BackfillsMainRole()
    {
        await EnableCommercialModeAsync();
        _db.FuelTanks.Add(new FuelTank
        {
            Id = 50, SubStationId = 1, FuelLookupId = 1, Name = "Legacy main", Code = "LEG-01",
            CapacityLiters = 5000, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var update = new UpdateFuelTankDto(null, null, "Renamed legacy main", null, null, null, null, null, null, null, null, null, null);
        var result = await _tankService.UpdateAsync(50, update, 1);

        Assert.Equal(TankRole.Main, result.TankRole);
        Assert.Null(result.GeneratorId);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
