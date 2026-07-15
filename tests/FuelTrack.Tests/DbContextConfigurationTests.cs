using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Tests;

public class DbContextConfigurationTests
{
    private static DbContextOptions<FuelTrackDbContext> CreateInMemoryOptions(string dbName)
    {
        return new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
    }

    private static FuelTrackDbContext CreateContext(string dbName)
    {
        var options = CreateInMemoryOptions(dbName);
        var context = new FuelTrackDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task Can_Insert_And_Retrieve_All_ParentEntities()
    {
        using var context = CreateContext(nameof(Can_Insert_And_Retrieve_All_ParentEntities));

        var branch = new Branch { Name = "Main Branch", Code = "MB-01" };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();

        var station = new Station { BranchId = branch.Id, Name = "Main Station", Code = "MS-01" };
        context.Stations.Add(station);
        await context.SaveChangesAsync();

        var subStation = new SubStation { Name = "SS-1", Code = "SS-01", StationId = station.Id };
        context.SubStations.Add(subStation);
        await context.SaveChangesAsync();

        var fuelCat = new LookupCategory { Name = "FuelType" };
        context.LookupCategories.Add(fuelCat);
        await context.SaveChangesAsync();

        var unitCat = new LookupCategory { Name = "Unit" };
        context.LookupCategories.Add(unitCat);
        await context.SaveChangesAsync();

        var unitLookup = new Lookup { Name = "kW", CategoryId = unitCat.Id, Code = "KW" };
        context.Lookups.Add(unitLookup);
        await context.SaveChangesAsync();

        var gen = new Generator
        {
            Name = "Gen-1",
            SerialNumber = "SN",
            SubStationId = subStation.Id,
            NominalPower = 500,
            NominalPowerUnitLookupId = unitLookup.Id,
            ActualPowerKW = 480,
            FuelConsumptionRatePerHour = 120m
        };
        context.Generators.Add(gen);
        await context.SaveChangesAsync();

        var fuelLookup = new Lookup { Name = "Diesel", CategoryId = fuelCat.Id, Code = "DIESEL" };
        context.Lookups.Add(fuelLookup);
        await context.SaveChangesAsync();

        var fuelTank = new FuelTank
        {
            Name = "FT-1",
            Code = "FT-001",
            SubStationId = subStation.Id,
            CapacityLiters = 10000,
            FuelLookupId = fuelLookup.Id
        };
        context.FuelTanks.Add(fuelTank);
        await context.SaveChangesAsync();

        var oilCat = new LookupCategory { Name = "OilType" };
        context.LookupCategories.Add(oilCat);
        await context.SaveChangesAsync();

        var oilLookup = new Lookup { Name = "SAE 40", CategoryId = oilCat.Id, Code = "SAE40" };
        context.Lookups.Add(oilLookup);
        await context.SaveChangesAsync();

        var oil = new Oil
        {
            OilLookupId = oilLookup.Id,
            CurrentStock = 1000,
            MinStock = 100,
            SubStationId = subStation.Id
        };
        context.Oils.Add(oil);
        await context.SaveChangesAsync();

        var user = new User
        {
            UserName = "admin",
            PasswordHash = "hash",
            FullName = "Admin",
            Email = "admin@test.com"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        Assert.Equal(1, await context.Branches.CountAsync());
        Assert.Equal(1, await context.Stations.CountAsync());
        Assert.Equal(1, await context.SubStations.CountAsync());
        Assert.Equal(1, await context.Generators.CountAsync());
        Assert.Equal(1, await context.FuelTanks.CountAsync());
        Assert.Equal(1, await context.Oils.CountAsync());
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Fact(Skip = "InMemory does not enforce unique constraints—requires SQL Server")]
    public async Task DailyReading_Requires_Unique_GeneratorId_ReadingDate_Shift()
    {
        using var context = CreateContext(nameof(DailyReading_Requires_Unique_GeneratorId_ReadingDate_Shift));

        var genId = 999;
        var date = DateTime.UtcNow;
        var dr1 = new DailyReading
        {
            GeneratorId = genId,
            ReadingDate = date,
            PrevEnergyKWh = 0,
            CurrentEnergyKWh = 5000,
            PrevRunningHours = 0,
            CurrentRunningHours = 10
        };
        context.DailyReadings.Add(dr1);
        await context.SaveChangesAsync();

        var dr2 = new DailyReading
        {
            GeneratorId = genId,
            ReadingDate = date,
            PrevEnergyKWh = 5000,
            CurrentEnergyKWh = 5500,
            PrevRunningHours = 10,
            CurrentRunningHours = 11
        };
        context.DailyReadings.Add(dr2);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.NotNull(ex);
    }

    [Fact(Skip = "InMemory does not enforce unique constraints—requires SQL Server")]
    public async Task SubStationReading_Requires_Unique_SubStationId_ReadingDate()
    {
        using var context = CreateContext(nameof(SubStationReading_Requires_Unique_SubStationId_ReadingDate));

        var subId = 999;
        var date = DateTime.UtcNow;
        var sr1 = new SubStationReading
        {
            SubStationId = subId,
            ReadingDate = date
        };
        context.SubStationReadings.Add(sr1);
        await context.SaveChangesAsync();

        var sr2 = new SubStationReading
        {
            SubStationId = subId,
            ReadingDate = date
        };
        context.SubStationReadings.Add(sr2);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact(Skip = "InMemory does not enforce unique constraints—requires SQL Server")]
    public async Task Oil_Requires_Unique_SubStationId_OilLookupId()
    {
        using var context = CreateContext(nameof(Oil_Requires_Unique_SubStationId_OilLookupId));

        var subId = 999;
        var oilId = 998;
        context.Oils.Add(new Oil
        {
            OilLookupId = oilId,
            CurrentStock = 1000,
            MinStock = 100,
            SubStationId = subId
        });
        await context.SaveChangesAsync();

        context.Oils.Add(new Oil
        {
            OilLookupId = oilId,
            CurrentStock = 2000,
            MinStock = 200,
            SubStationId = subId
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact(Skip = "InMemory does not enforce unique constraints—requires SQL Server")]
    public async Task FuelTank_Requires_Unique_SubStationId_FuelLookupId()
    {
        using var context = CreateContext(nameof(FuelTank_Requires_Unique_SubStationId_FuelLookupId));

        var subId = 999;
        var fuelId = 998;
        context.FuelTanks.Add(new FuelTank
        {
            Name = "FT-1",
            Code = "FT-001",
            SubStationId = subId,
            CapacityLiters = 10000,
            FuelLookupId = fuelId
        });
        await context.SaveChangesAsync();

        context.FuelTanks.Add(new FuelTank
        {
            Name = "FT-2",
            Code = "FT-002",
            SubStationId = subId,
            CapacityLiters = 20000,
            FuelLookupId = fuelId
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact(Skip = "InMemory does not enforce unique constraints—requires SQL Server")]
    public async Task UserScope_Junction_Unique()
    {
        using var context = CreateContext(nameof(UserScope_Junction_Unique));

        var user = new User { UserName = "u", PasswordHash = "h", FullName = "U", Email = "u@t.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var branch = new Branch { Name = "B", Code = "B" };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();

        var station = new Station { BranchId = branch.Id, Name = "S", Code = "S" };
        context.Stations.Add(station);
        await context.SaveChangesAsync();

        var ss = new SubStation { Name = "SS", Code = "SS", StationId = station.Id };
        context.SubStations.Add(ss);
        await context.SaveChangesAsync();

        context.UserScopes.Add(new UserScope { UserId = user.Id, BranchId = branch.Id, StationId = station.Id, SubStationId = ss.Id });
        await context.SaveChangesAsync();

        context.UserScopes.Add(new UserScope { UserId = user.Id, BranchId = branch.Id, StationId = station.Id, SubStationId = ss.Id });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Generated_Id_On_Insert()
    {
        using var context = CreateContext(nameof(Generated_Id_On_Insert));

        var branch = new Branch { Name = "B", Code = "B" };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();

        Assert.NotEqual(0, branch.Id);
    }


}
