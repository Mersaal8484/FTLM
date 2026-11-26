using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Tests.Services;

public sealed class SqliteFuelTankSchemaRepairTests
{
    [Fact]
    public async Task ApplyAsync_ExistingDatabase_CreatesGeneratorMaintenanceOilConfigurationTable()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>().UseSqlite(connection).Options;
        await using var db = new FuelTrackDbContext(options);
        await db.Database.EnsureCreatedAsync();

        await db.Database.ExecuteSqlRawAsync("DROP TABLE \"GeneratorMaintenanceOilConfigurations\";");

        await SqliteFuelTankSchemaRepair.ApplyAsync(db);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'GeneratorMaintenanceOilConfigurations';";
        Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync()));
    }
}
