using FuelTrack.Application.DTOs.Generators;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FuelTrack.Tests.Services;

public class GeneratorDailyTankLinkingTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly GeneratorService _generatorService;
    private readonly FuelTankService _tankService;
    private readonly CommercialStationModeService _commercialMode;
    private readonly TestCurrentUser _currentUser = new(1, "Admin");

    public GeneratorDailyTankLinkingTests()
    {
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new FuelTrackDbContext(options);

        // Seed basic station and lookups
        var branch = new Branch { Id = 1, Name = "Branch 1" };
        var station = new Station { Id = 1, BranchId = 1, Name = "Station 1" };
        var subStation = new SubStation { Id = 10, StationId = 1, Name = "Site 1" };
        _db.Branches.Add(branch);
        _db.Stations.Add(station);
        _db.SubStations.Add(subStation);

        var fuelLookup = new Lookup { Id = 1, Name = "Diesel", Code = "DIESEL", IsActive = true };
        _db.Lookups.Add(fuelLookup);
        _db.SaveChanges();

        _commercialMode = new CommercialStationModeService(_db);
        _generatorService = new GeneratorService(_db, new ScopeFilterService(), _currentUser, _commercialMode);
        _tankService = new FuelTankService(_db, _commercialMode);
    }

    private async Task EnableCommercialModeAsync()
    {
        await _commercialMode.ActivateAsync(new("Commercial Station", "CS-01"), 1);
    }

    [Fact]
    public async Task CreateAsync_Commercial_WithValidUnlinkedDailyTank_LinksTankAndCreatesGenerator()
    {
        await EnableCommercialModeAsync();
        var scope = await _commercialMode.GetScopeAsync();

        var tank = new FuelTank
        {
            SubStationId = scope!.SubStationId,
            FuelLookupId = 1,
            Name = "Daily Tank 1",
            Code = "DT-01",
            CapacityLiters = 1000,
            TankRole = TankRole.Daily,
            IsActive = true
        };
        _db.FuelTanks.Add(tank);
        await _db.SaveChangesAsync();

        var dto = new CreateGeneratorDto(
            SubStationId: 0,
            Name: "Generator 1",
            SerialNumber: "GEN-01",
            Model: "M1",
            Manufacturer: "Man1",
            DailyTankId: tank.Id);

        var result = await _generatorService.CreateAsync(dto, 1);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(tank.Id, result.Value.DailyTankId);
        Assert.Equal("Daily Tank 1", result.Value.DailyTankName);

        var reloadedTank = await _db.FuelTanks.FindAsync(tank.Id);
        Assert.Equal(result.Value.Id, reloadedTank!.GeneratorId);
    }

    [Fact]
    public async Task CreateAsync_Commercial_WithoutDailyTank_Fails()
    {
        await EnableCommercialModeAsync();

        var dto = new CreateGeneratorDto(
            SubStationId: 0,
            Name: "Generator No Tank",
            SerialNumber: "GEN-NT",
            Model: "M1",
            Manufacturer: "Man1",
            DailyTankId: null);

        var result = await _generatorService.CreateAsync(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Contains("daily tank", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_Commercial_WithNonExistentTank_ReturnsNotFound()
    {
        await EnableCommercialModeAsync();

        var dto = new CreateGeneratorDto(
            SubStationId: 0,
            Name: "Generator Bad Tank",
            SerialNumber: "GEN-BT",
            Model: "M1",
            Manufacturer: "Man1",
            DailyTankId: 99999);

        var result = await _generatorService.CreateAsync(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_Commercial_WithMainRoleTank_Fails()
    {
        await EnableCommercialModeAsync();
        var scope = await _commercialMode.GetScopeAsync();

        var mainTank = new FuelTank
        {
            SubStationId = scope!.SubStationId,
            FuelLookupId = 1,
            Name = "Main Tank",
            Code = "MT-01",
            CapacityLiters = 5000,
            TankRole = TankRole.Main,
            IsActive = true
        };
        _db.FuelTanks.Add(mainTank);
        await _db.SaveChangesAsync();

        var dto = new CreateGeneratorDto(
            SubStationId: 0,
            Name: "Generator Main Tank",
            SerialNumber: "GEN-MT",
            Model: "M1",
            Manufacturer: "Man1",
            DailyTankId: mainTank.Id);

        var result = await _generatorService.CreateAsync(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Contains("not a daily tank", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_Commercial_WithTankFromDifferentStation_Fails()
    {
        await EnableCommercialModeAsync();

        var otherSubStation = new SubStation { Id = 99, StationId = 1, Name = "Other Site" };
        _db.SubStations.Add(otherSubStation);

        var tank = new FuelTank
        {
            SubStationId = 99,
            FuelLookupId = 1,
            Name = "Other Station DT",
            Code = "DT-OTHER",
            CapacityLiters = 1000,
            TankRole = TankRole.Daily,
            IsActive = true
        };
        _db.FuelTanks.Add(tank);
        await _db.SaveChangesAsync();

        var dto = new CreateGeneratorDto(
            SubStationId: 0,
            Name: "Generator Other Station",
            SerialNumber: "GEN-OS",
            Model: "M1",
            Manufacturer: "Man1",
            DailyTankId: tank.Id);

        var result = await _generatorService.CreateAsync(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Contains("does not belong to this station", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_Commercial_WithAlreadyLinkedTank_Fails()
    {
        await EnableCommercialModeAsync();
        var scope = await _commercialMode.GetScopeAsync();

        var existingGen = new Generator
        {
            SubStationId = scope!.SubStationId,
            Name = "Existing Gen",
            SerialNumber: "EXIST-01",
            IsActive = true
        };
        _db.Generators.Add(existingGen);
        await _db.SaveChangesAsync();

        var tank = new FuelTank
        {
            SubStationId = scope.SubStationId,
            FuelLookupId = 1,
            Name = "Taken DT",
            Code = "DT-TAKEN",
            CapacityLiters = 1000,
            TankRole = TankRole.Daily,
            GeneratorId = existingGen.Id,
            IsActive = true
        };
        _db.FuelTanks.Add(tank);
        await _db.SaveChangesAsync();

        var dto = new CreateGeneratorDto(
            SubStationId: 0,
            Name: "New Generator",
            SerialNumber: "NEW-GEN",
            Model: "M1",
            Manufacturer: "Man1",
            DailyTankId: tank.Id);

        var result = await _generatorService.CreateAsync(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Contains("already linked to another generator", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_Commercial_ReLinkToNewDailyTank_ReleasesOldAndClaimsNew()
    {
        await EnableCommercialModeAsync();
        var scope = await _commercialMode.GetScopeAsync();

        var tank1 = new FuelTank
        {
            SubStationId = scope!.SubStationId,
            FuelLookupId = 1,
            Name = "Old DT",
            Code = "DT-OLD",
            CapacityLiters = 1000,
            TankRole = TankRole.Daily,
            IsActive = true
        };
        var tank2 = new FuelTank
        {
            SubStationId = scope.SubStationId,
            FuelLookupId = 1,
            Name = "New DT",
            Code = "DT-NEW",
            CapacityLiters = 1500,
            TankRole = TankRole.Daily,
            IsActive = true
        };
        _db.FuelTanks.AddRange(tank1, tank2);
        await _db.SaveChangesAsync();

        var createDto = new CreateGeneratorDto(
            SubStationId: 0,
            Name: "Generator",
            SerialNumber: "GEN-RELINK",
            Model: "M1",
            Manufacturer: "Man1",
            DailyTankId: tank1.Id);
        var created = await _generatorService.CreateAsync(createDto, 1);
        Assert.True(created.IsSuccess);

        // Re-link to tank2
        var updateDto = new UpdateGeneratorDto(
            SubStationId: null,
            Name: "Generator Updated",
            SerialNumber: null,
            Model: null,
            Manufacturer: null,
            NominalPower: null,
            NominalPowerUnitLookupId: null,
            ActualPowerKW: null,
            FuelConsumptionRatePerHour: null,
            OwnerName: null,
            GeneratorTypeLookupId: null,
            VoltageLookupId: null,
            FrequencyLookupId: null,
            PowerFactor: null,
            OwnershipTypeLookupId: null,
            StatusLookupId: null,
            InstallationDate: null,
            Notes: null,
            GeneratorTypeLookupName: null,
            VoltageLookupName: null,
            FrequencyLookupName: null,
            OwnershipTypeLookupName: null,
            StatusLookupName: null,
            IsActive: true,
            DailyTankId: tank2.Id);

        var updated = await _generatorService.UpdateAsync(created.Value!.Id, updateDto, 1);
        Assert.True(updated.IsSuccess);
        Assert.Equal(tank2.Id, updated.Value!.DailyTankId);
        Assert.Equal("New DT", updated.Value.DailyTankName);

        var reloadedTank1 = await _db.FuelTanks.FindAsync(tank1.Id);
        var reloadedTank2 = await _db.FuelTanks.FindAsync(tank2.Id);

        Assert.Null(reloadedTank1!.GeneratorId);
        Assert.Equal(created.Value.Id, reloadedTank2!.GeneratorId);
    }

    [Fact]
    public async Task DeleteAsync_Commercial_ReleasesDailyTank()
    {
        await EnableCommercialModeAsync();
        var scope = await _commercialMode.GetScopeAsync();

        var tank = new FuelTank
        {
            SubStationId = scope!.SubStationId,
            FuelLookupId = 1,
            Name = "DT to Release",
            Code = "DT-REL",
            CapacityLiters = 1000,
            TankRole = TankRole.Daily,
            IsActive = true
        };
        _db.FuelTanks.Add(tank);
        await _db.SaveChangesAsync();

        var createDto = new CreateGeneratorDto(
            SubStationId: 0,
            Name: "Generator to Delete",
            SerialNumber: "GEN-DEL",
            DailyTankId: tank.Id);
        var created = await _generatorService.CreateAsync(createDto, 1);
        Assert.True(created.IsSuccess);

        var deleteResult = await _generatorService.DeleteAsync(created.Value!.Id, 1);
        Assert.True(deleteResult.IsSuccess);

        var reloadedTank = await _db.FuelTanks.FindAsync(tank.Id);
        Assert.Null(reloadedTank!.GeneratorId);
    }

    [Fact]
    public async Task FuelTankService_DeleteAsync_DailyTankLinkedToActiveGenerator_Blocked()
    {
        await EnableCommercialModeAsync();
        var scope = await _commercialMode.GetScopeAsync();

        var generator = new Generator
        {
            SubStationId = scope!.SubStationId,
            Name = "Active Gen",
            SerialNumber: "ACT-01",
            IsActive = true
        };
        _db.Generators.Add(generator);
        await _db.SaveChangesAsync();

        var tank = new FuelTank
        {
            SubStationId = scope.SubStationId,
            FuelLookupId = 1,
            Name = "Linked Tank",
            Code = "LT-01",
            CapacityLiters = 1000,
            TankRole = TankRole.Daily,
            GeneratorId = generator.Id,
            IsActive = true
        };
        _db.FuelTanks.Add(tank);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _tankService.DeleteAsync(tank.Id));
        Assert.Contains("Cannot delete a daily tank that is assigned to an active generator", ex.Message);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private sealed class ScopeFilterService : IUserScopeFilterService
    {
        public Task<List<int>> GetAccessibleSubStationIdsAsync(int userId) => Task.FromResult(new List<int> { 1, 10, 99 });
        public Task<List<int>> GetAccessibleStationIdsAsync(int userId) => Task.FromResult(new List<int> { 1 });
        public Task<List<int>> GetAccessibleBranchIdsAsync(int userId) => Task.FromResult(new List<int> { 1 });
        public Task<List<int>> GetAccessibleMeterIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetUserIdsWithAccessToSubStationAsync(int subStationId) => Task.FromResult(new List<int>());
        public Task<bool> HasAccessToSubStationAsync(int userId, int subStationId) => Task.FromResult(true);
        public Task<bool> HasFullAccessToStationAsync(int userId, int stationId) => Task.FromResult(true);
    }

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public TestCurrentUser(int? userId, string? role) { UserId = userId; Role = role; }
        public int? UserId { get; }
        public string? Role { get; }
        public string? Username => "test";
        public bool HasRole(string role) => Role == role;
        public bool HasAnyAdminRole => Role == "Admin";
        public bool IsStationScopeRestricted => false;
    }
}
