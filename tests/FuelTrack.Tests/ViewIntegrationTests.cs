using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Tests;

public class ViewIntegrationTests
{
    private const string ConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=FuelTrackDb_Views_Test;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact(Skip = "Requires SQL Server instance — run manually")]
    public async Task LatestApprovedReadingPerGenerator_Returns_RankedResults()
    {
        using var context = CreateSqlServerContext();
        await SeedGeneratorWithReadings(context);

        var results = await context.Database
            .SqlQuery<LatestReadingResult>(
                $"SELECT * FROM [dbo].[vw_LatestApprovedReadingPerGenerator]")
            .ToListAsync();

        Assert.NotEmpty(results);
        foreach (var r in results)
        {
            Assert.Equal(1, r.Rn);
        }
    }

    [Fact(Skip = "Requires SQL Server instance — run manually")]
    public async Task CurrentFuelBalance_Returns_Rows()
    {
        using var context = CreateSqlServerContext();
        await SeedFuelBalanceData(context);

        var results = await context.Database
            .SqlQuery<FuelBalanceResult>(
                $"SELECT * FROM [dbo].[vw_CurrentFuelBalance]")
            .ToListAsync();

        Assert.NotEmpty(results);
    }

    [Fact(Skip = "Requires SQL Server instance — run manually")]
    public async Task CurrentOilBalance_Returns_Rows()
    {
        using var context = CreateSqlServerContext();
        await SeedOilBalanceData(context);

        var results = await context.Database
            .SqlQuery<OilBalanceResult>(
                $"SELECT * FROM [dbo].[vw_CurrentOilBalance]")
            .ToListAsync();

        Assert.NotEmpty(results);
    }

    [Fact(Skip = "Requires SQL Server instance — run manually")]
    public async Task GeneratorPeriodicEfficiency_Returns_Rows()
    {
        using var context = CreateSqlServerContext();
        await SeedEfficiencyData(context);

        var results = await context.Database
            .SqlQuery<EfficiencyResult>(
                $"SELECT * FROM [dbo].[vw_GeneratorPeriodicEfficiency]")
            .ToListAsync();

        Assert.NotEmpty(results);
    }

    [Theory(Skip = "Requires SQL Server instance — run manually")]
    [InlineData("2026-01-01", "2026-12-31")]
    public async Task GeneratorStatsByDateRange_Returns_Rows(string from, string to)
    {
        using var context = CreateSqlServerContext();
        await SeedGeneratorWithReadings(context);

        var genId = await context.Generators.Select(g => g.Id).FirstAsync();
        var results = await context.Database
            .SqlQuery<GeneratorStatResult>(
                $"SELECT * FROM [dbo].[fn_GeneratorStatsByDateRange]({genId}, '{from}', '{to}')")
            .ToListAsync();

        Assert.NotEmpty(results);
    }

    private static FuelTrackDbContext CreateSqlServerContext()
    {
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        var context = new FuelTrackDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.Migrate();
        return context;
    }

    private static async Task<int> SeedBranch(FuelTrackDbContext context)
    {
        var branch = new Branch { Name = "Main", Code = "MB" };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch.Id;
    }

    private static async Task<Lookup> SeedLookupCategoryAndLookup(FuelTrackDbContext context, int? categoryId, string code, string name)
    {
        LookupCategory? category = null;
        if (categoryId == null)
        {
            category = new LookupCategory { Name = code, DisplayName = code };
            context.LookupCategories.Add(category);
            await context.SaveChangesAsync();
        }
        else
        {
            category = await context.LookupCategories.FindAsync(categoryId.Value);
        }

        var lookup = new Lookup
        {
            CategoryId = category!.Id,
            Code = code,
            Name = name
        };
        context.Lookups.Add(lookup);
        await context.SaveChangesAsync();
        return lookup;
    }

    private static async Task SeedGeneratorWithReadings(FuelTrackDbContext context)
    {
        var branchId = await SeedBranch(context);
        var station = new Station { BranchId = branchId, Name = "S", Code = "S" };
        context.Stations.Add(station);
        await context.SaveChangesAsync();

        var ss = new SubStation { Name = "SS", Code = "SS", StationId = station.Id };
        context.SubStations.Add(ss);
        await context.SaveChangesAsync();

        var fuelLookup = await SeedLookupCategoryAndLookup(context, null, "FuelType", "Diesel");
        var readingStatusLookup = await SeedLookupCategoryAndLookup(context, fuelLookup.CategoryId, "ReadingStatus", "Approved");

        var gen = new Generator
        {
            Name = "Gen-1",
            SerialNumber = "G-001",
            SubStationId = ss.Id,
            NominalPower = 500,
            NominalPowerUnitLookupId = fuelLookup.Id,
            ActualPowerKW = 475,
            FuelConsumptionRatePerHour = 120
        };
        context.Generators.Add(gen);
        await context.SaveChangesAsync();

        for (int i = 1; i <= 3; i++)
        {
            context.DailyReadings.Add(new DailyReading
            {
                GeneratorId = gen.Id,
                ReadingDate = DateTime.UtcNow.AddDays(-i),
                PrevEnergyKWh = 0,
                CurrentEnergyKWh = 5000 * i,
                PrevRunningHours = 0,
                CurrentRunningHours = 10 * i,
                FuelLookupId = fuelLookup.Id,
                StandardFuelRate = 120,
                OilLookupId = fuelLookup.Id,
                OilUsedLiters = 5 * i,
                ReadingStatusLookupId = readingStatusLookup.Id
            });
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedFuelBalanceData(FuelTrackDbContext context)
    {
        var branchId = await SeedBranch(context);
        var station = new Station { BranchId = branchId, Name = "S", Code = "S" };
        context.Stations.Add(station);
        await context.SaveChangesAsync();

        var ss = new SubStation { Name = "SS", Code = "SS", StationId = station.Id };
        context.SubStations.Add(ss);
        await context.SaveChangesAsync();

        var fuelLookup = await SeedLookupCategoryAndLookup(context, null, "FuelType", "Diesel");
        var transactionTypeLookup = await SeedLookupCategoryAndLookup(context, fuelLookup.CategoryId, "TransactionType", "Purchase");

        var fuelTank = new FuelTank
        {
            Name = "FT",
            Code = "FT",
            SubStationId = ss.Id,
            CapacityLiters = 10000,
            FuelLookupId = fuelLookup.Id
        };
        context.FuelTanks.Add(fuelTank);
        await context.SaveChangesAsync();

        context.FuelTransactions.Add(new FuelTransaction
        {
            SubStationId = ss.Id,
            FuelLookupId = fuelLookup.Id,
            TransactionTypeLookupId = transactionTypeLookup.Id,
            TransactionDate = DateTime.UtcNow,
            QuantityLiters = 5000,
            Reference = "INV-001"
        });
        await context.SaveChangesAsync();
    }

    private static async Task SeedOilBalanceData(FuelTrackDbContext context)
    {
        var branchId = await SeedBranch(context);
        var station = new Station { BranchId = branchId, Name = "S", Code = "S" };
        context.Stations.Add(station);
        await context.SaveChangesAsync();

        var ss = new SubStation { Name = "SS", Code = "SS", StationId = station.Id };
        context.SubStations.Add(ss);
        await context.SaveChangesAsync();

        var oilLookup = await SeedLookupCategoryAndLookup(context, null, "OilType", "SAE 40");
        var transactionTypeLookup = await SeedLookupCategoryAndLookup(context, oilLookup.CategoryId, "TransactionType", "Purchase");

        var oil = new Oil
        {
            OilLookupId = oilLookup.Id,
            CurrentStock = 1000,
            MinStock = 100,
            SubStationId = ss.Id
        };
        context.Oils.Add(oil);
        await context.SaveChangesAsync();

        context.OilTransactions.Add(new OilTransaction
        {
            SubStationId = ss.Id,
            OilLookupId = oilLookup.Id,
            TransactionTypeLookupId = transactionTypeLookup.Id,
            TransactionDate = DateTime.UtcNow,
            QuantityLiters = 500,
            Reference = "INV-O-001"
        });
        await context.SaveChangesAsync();
    }

    private static async Task SeedEfficiencyData(FuelTrackDbContext context)
    {
        var branchId = await SeedBranch(context);
        var station = new Station { BranchId = branchId, Name = "S", Code = "S" };
        context.Stations.Add(station);
        await context.SaveChangesAsync();

        var ss = new SubStation { Name = "SS", Code = "SS", StationId = station.Id };
        context.SubStations.Add(ss);
        await context.SaveChangesAsync();

        var gen = new Generator
        {
            Name = "Gen-1",
            SerialNumber = "G-001",
            SubStationId = ss.Id,
            NominalPower = 500,
            NominalPowerUnitLookupId = (await SeedLookupCategoryAndLookup(context, null, "Unit", "kW")).Id,
            ActualPowerKW = 475,
            FuelConsumptionRatePerHour = 120
        };
        context.Generators.Add(gen);
        await context.SaveChangesAsync();

        var fuelLookup = await SeedLookupCategoryAndLookup(context, gen.NominalPowerUnitLookupId, "FuelType", "Diesel");
        var periodTypeLookup = await SeedLookupCategoryAndLookup(context, fuelLookup.CategoryId, "PeriodType", "Monthly");

        var pi = new PeriodicInventory
        {
            SubStationId = ss.Id,
            InventoryDate = new DateTime(2026, 1, 31),
            Year = 2026,
            Month = 1,
            PeriodTypeLookupId = periodTypeLookup.Id
        };
        context.PeriodicInventories.Add(pi);
        await context.SaveChangesAsync();

        context.PeriodicInventoryFuels.Add(new PeriodicInventoryFuel
        {
            PeriodicInventoryId = pi.Id,
            FuelLookupId = fuelLookup.Id,
            ActualStockLiters = 2000
        });
        await context.SaveChangesAsync();

        var readingStatusLookup = await SeedLookupCategoryAndLookup(context, fuelLookup.CategoryId, "ReadingStatus", "Approved");

        for (int d = 1; d <= 31; d++)
        {
            context.DailyReadings.Add(new DailyReading
            {
                GeneratorId = gen.Id,
                ReadingDate = new DateTime(2026, 1, d),
                PrevEnergyKWh = 0,
                CurrentEnergyKWh = 5000,
                PrevRunningHours = 0,
                CurrentRunningHours = 10,
                FuelLookupId = fuelLookup.Id,
                StandardFuelRate = 120,
                OilLookupId = fuelLookup.Id,
                OilUsedLiters = 5,
                ReadingStatusLookupId = readingStatusLookup.Id
            });
        }
        await context.SaveChangesAsync();
    }

    internal class LatestReadingResult
    {
        public int GeneratorId { get; set; }
        public string? GeneratorName { get; set; }
        public string? GeneratorCode { get; set; }
        public int? SubStationId { get; set; }
        public string? SubStationName { get; set; }
        public int? StationId { get; set; }
        public string? StationName { get; set; }
        public int? DailyReadingId { get; set; }
        public DateOnly? ReadingDate { get; set; }
        public double? PrevEnergyKWh { get; set; }
        public double? CurrentEnergyKWh { get; set; }
        public double? PrevRunningHours { get; set; }
        public double? CurrentRunningHours { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public int? VerifiedById { get; set; }
        public long Rn { get; set; }
    }

    internal class FuelBalanceResult
    {
        public int FuelTankId { get; set; }
        public string? TankName { get; set; }
        public string? TankCode { get; set; }
        public string? FuelName { get; set; }
        public int? SubStationId { get; set; }
        public double? CurrentBalanceLiters { get; set; }
    }

    internal class OilBalanceResult
    {
        public int OilTankId { get; set; }
        public string? TankName { get; set; }
        public string? TankCode { get; set; }
        public string? OilName { get; set; }
        public int? SubStationId { get; set; }
        public double? CurrentBalanceLiters { get; set; }
    }

    internal class EfficiencyResult
    {
        public int GeneratorId { get; set; }
        public string? GeneratorName { get; set; }
        public string? GeneratorCode { get; set; }
        public int PeriodicInventoryId { get; set; }
        public DateTime InventoryDate { get; set; }
        public double? ConsumedFuelLiters { get; set; }
        public double? ProducedEnergyKWh { get; set; }
        public double? EfficiencyKWhPerLiter { get; set; }
    }

    internal class GeneratorStatResult
    {
        public double? TotalRunHours { get; set; }
        public double? TotalEnergyKWh { get; set; }
        public double? AvgLoadKW { get; set; }
        public int? ReadingCount { get; set; }
    }
}
