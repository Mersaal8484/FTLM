using FuelTrack.Application.DTOs.Generators;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.Data.Sqlite;
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
        var scopeFilter = new ScopeFilterService();
        _generatorService = new GeneratorService(_db, scopeFilter, _currentUser, _commercialMode);
        _tankService = new FuelTankService(_db, scopeFilter, _currentUser, _commercialMode);
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

        var otherSubStation = new SubStation { Id = 99, StationId = 1, Name = "Other Site", IsActive = false };
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
            SerialNumber = "EXIST-01",
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
            Model: "M1",
            Manufacturer: "Man1",
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
            SerialNumber = "ACT-01",
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
        Assert.Contains("daily tank of an active generator cannot be deleted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_Commercial_WithInactiveDailyTank_Fails()
    {
        await EnableCommercialModeAsync();
        var scope = await _commercialMode.GetScopeAsync();

        var inactiveTank = new FuelTank
        {
            SubStationId = scope!.SubStationId,
            FuelLookupId = 1,
            Name = "Inactive DT",
            Code = "DT-INACT",
            CapacityLiters = 1000,
            TankRole = TankRole.Daily,
            IsActive = false
        };
        _db.FuelTanks.Add(inactiveTank);
        await _db.SaveChangesAsync();

        var dto = new CreateGeneratorDto(0, "Gen 1", "G-01", "M1", "Man1", DailyTankId: inactiveTank.Id);
        var result = await _generatorService.CreateAsync(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Contains("inactive", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_Commercial_DeactivatingGenerator_ReleasesDailyTank()
    {
        await EnableCommercialModeAsync();
        var scope = await _commercialMode.GetScopeAsync();

        var tank = new FuelTank
        {
            SubStationId = scope!.SubStationId,
            FuelLookupId = 1,
            Name = "DT Active",
            Code = "DT-ACT",
            CapacityLiters = 1000,
            TankRole = TankRole.Daily,
            IsActive = true
        };
        _db.FuelTanks.Add(tank);
        await _db.SaveChangesAsync();

        var created = await _generatorService.CreateAsync(
            new CreateGeneratorDto(0, "Gen Deactivate", "G-DEACT", "M1", "Man1", DailyTankId: tank.Id), 1);
        Assert.True(created.IsSuccess);

        var updateDto = new UpdateGeneratorDto(
            null, "Gen Deactivate", null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null,
            IsActive: false,
            DailyTankId: tank.Id);
        var updated = await _generatorService.UpdateAsync(created.Value!.Id, updateDto, 1);

        Assert.True(updated.IsSuccess);
        Assert.False(updated.Value!.IsActive);
        Assert.Null(updated.Value.DailyTankId);

        var reloadedTank = await _db.FuelTanks.FindAsync(tank.Id);
        Assert.Null(reloadedTank!.GeneratorId);
    }

    [Fact]
    public async Task UpdateAsync_Commercial_ActiveGeneratorWithoutDailyTank_Fails()
    {
        await EnableCommercialModeAsync();
        var scope = await _commercialMode.GetScopeAsync();

        var unlinkedGen = new Generator
        {
            SubStationId = scope!.SubStationId,
            Name = "Legacy Unlinked Gen",
            SerialNumber = "G-LEG",
            IsActive = true
        };
        _db.Generators.Add(unlinkedGen);
        await _db.SaveChangesAsync();

        var updateDto = new UpdateGeneratorDto(
            null, "Renamed", null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null,
            IsActive: true,
            DailyTankId: null);
        var result = await _generatorService.UpdateAsync(unlinkedGen.Id, updateDto, 1);

        Assert.False(result.IsSuccess);
        Assert.Contains("daily tank", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sqlite_UpdateAsync_RelinkDailyTank_TwoPhaseSaveChanges_RespectsFilteredUniqueIndex()
    {
        var (connection, db, subStationId, fuelLookupId) = await CreateSqliteCommercialAsync();
        await using (connection)
        await using (db)
        {
            var tank1 = new FuelTank
            {
                SubStationId = subStationId,
                FuelLookupId = fuelLookupId,
                Name = "SQLite Daily 1",
                Code = "SD-1",
                CapacityLiters = 1000,
                TankRole = TankRole.Daily,
                IsActive = true
            };
            var tank2 = new FuelTank
            {
                SubStationId = subStationId,
                FuelLookupId = fuelLookupId,
                Name = "SQLite Daily 2",
                Code = "SD-2",
                CapacityLiters = 1000,
                TankRole = TankRole.Daily,
                IsActive = true
            };
            db.FuelTanks.AddRange(tank1, tank2);
            await db.SaveChangesAsync();

            var service = new GeneratorService(db, new ScopeFilterService(), _currentUser, new CommercialStationModeService(db));
            var created = await service.CreateAsync(new CreateGeneratorDto(
                SubStationId: 0,
                Name: "SQLite Gen",
                SerialNumber: "SQL-GEN-1",
                Model: "M1",
                Manufacturer: "Man1",
                DailyTankId: tank1.Id), 1);

            Assert.True(created.IsSuccess);

            var relinked = await service.UpdateAsync(created.Value!.Id, new UpdateGeneratorDto(
                null, "SQLite Gen", null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null, null, null, null,
                IsActive: true,
                DailyTankId: tank2.Id), 1);

            Assert.True(relinked.IsSuccess);
            Assert.Equal(tank2.Id, relinked.Value!.DailyTankId);

            var reloadedTank1 = await db.FuelTanks.AsNoTracking().SingleAsync(t => t.Id == tank1.Id);
            var reloadedTank2 = await db.FuelTanks.AsNoTracking().SingleAsync(t => t.Id == tank2.Id);
            Assert.Null(reloadedTank1.GeneratorId);
            Assert.Equal(created.Value.Id, reloadedTank2.GeneratorId);

            // Confirm the SQLite filtered unique index (UX_FuelTanks_ActiveDailyGenerator) is active
            db.FuelTanks.Add(new FuelTank
            {
                SubStationId = subStationId,
                FuelLookupId = fuelLookupId,
                Name = "Duplicate Claim",
                Code = "SD-DUP",
                CapacityLiters = 1000,
                TankRole = TankRole.Daily,
                GeneratorId = created.Value.Id,
                IsActive = true
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task Sqlite_TwoGeneratorsClaimingSameTank_SecondFailsAndRollsBack()
    {
        var (connection, db1, subStationId, fuelLookupId) = await CreateSqliteCommercialAsync();
        await using (connection)
        await using (db1)
        {
            var tank = new FuelTank
            {
                SubStationId = subStationId,
                FuelLookupId = fuelLookupId,
                Name = "Contested Daily Tank",
                Code = "SD-CONT",
                CapacityLiters = 1000,
                TankRole = TankRole.Daily,
                IsActive = true
            };
            db1.FuelTanks.Add(tank);
            await db1.SaveChangesAsync();

            var options2 = new DbContextOptionsBuilder<FuelTrackDbContext>().UseSqlite(connection).Options;
            await using var db2 = new FuelTrackDbContext(options2);

            // Pre-load tank into db2's change tracker before generator 1 claims it so the
            // inside-transaction reload + re-check is genuinely exercised.
            var trackedBeforeClaim = await db2.FuelTanks.FindAsync(tank.Id);
            Assert.NotNull(trackedBeforeClaim);
            Assert.Null(trackedBeforeClaim!.GeneratorId);

            var service1 = new GeneratorService(db1, new ScopeFilterService(), _currentUser, new CommercialStationModeService(db1));
            var service2 = new GeneratorService(db2, new ScopeFilterService(), _currentUser, new CommercialStationModeService(db2));

            var first = await service1.CreateAsync(new CreateGeneratorDto(
                SubStationId: 0, Name: "Gen 1", SerialNumber: "G-C1", Model: "M1", Manufacturer: "Man1", DailyTankId: tank.Id), 1);
            Assert.True(first.IsSuccess);

            var second = await service2.CreateAsync(new CreateGeneratorDto(
                SubStationId: 0, Name: "Gen 2", SerialNumber: "G-C2", Model: "M1", Manufacturer: "Man1", DailyTankId: tank.Id), 1);

            Assert.False(second.IsSuccess);
            Assert.Contains("already linked", second.Error!, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1, await db1.Generators.CountAsync());
            var finalTank = await db1.FuelTanks.AsNoTracking().SingleAsync(t => t.Id == tank.Id);
            Assert.Equal(first.Value!.Id, finalTank.GeneratorId);
        }
    }

    private static async Task<(SqliteConnection Connection, FuelTrackDbContext Db, int SubStationId, int FuelLookupId)> CreateSqliteCommercialAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>().UseSqlite(connection).Options;
        var db = new FuelTrackDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var commercialMode = new CommercialStationModeService(db);
        var activated = await commercialMode.ActivateAsync(new("SQLite Commercial", "SQL-01"), 1);
        Assert.True(activated.IsSuccess);
        var scope = await commercialMode.GetScopeAsync();
        Assert.NotNull(scope);

        var fuelLookup = await db.Lookups.FirstOrDefaultAsync(l => l.IsActive);
        if (fuelLookup is null)
        {
            var cat = new LookupCategory { Name = "FuelType", DisplayName = "FuelType", IsActive = true };
            db.LookupCategories.Add(cat);
            await db.SaveChangesAsync();
            fuelLookup = new Lookup { CategoryId = cat.Id, Name = "Diesel", Code = "DIESEL", IsActive = true };
            db.Lookups.Add(fuelLookup);
            await db.SaveChangesAsync();
        }

        return (connection, db, scope!.SubStationId, fuelLookup.Id);
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
        public string? UserName => "test";
        public bool IsAuthenticated => UserId.HasValue;
        public bool IsAdmin => Role == "Admin";
        public bool IsSystemOwner => Role == "SystemOwner";
        public bool HasAnyAdminRole => Role == "Admin" || Role == "SystemOwner";
    }
}
