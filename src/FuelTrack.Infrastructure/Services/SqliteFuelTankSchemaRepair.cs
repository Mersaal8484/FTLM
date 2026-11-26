using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

/// <summary>
/// Keeps development SQLite databases created with older models compatible with
/// the commercial main/daily tank split. SQLite applications use EnsureCreated,
/// so EF migrations are not applied to an already-existing local file.
/// </summary>
public static class SqliteFuelTankSchemaRepair
{
    private const string FuelTypeIndexName = "IX_FuelTanks_SubStationId_FuelLookupId";

    public static async Task ApplyAsync(FuelTrackDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsSqlite())
            return;

        await dbContext.Database.ExecuteSqlRawAsync($"DROP INDEX IF EXISTS \"{FuelTypeIndexName}\";", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            $"CREATE UNIQUE INDEX IF NOT EXISTS \"{FuelTypeIndexName}\" " +
            "ON \"FuelTanks\" (\"SubStationId\", \"FuelLookupId\") " +
            "WHERE \"DeletedAt\" IS NULL AND (\"TankRole\" IS NULL OR \"TankRole\" <> 2);",
            cancellationToken);
    }
}
