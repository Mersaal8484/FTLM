using Microsoft.EntityFrameworkCore;
using System.Data;

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

        // Local development SQLite databases are created with EnsureCreated and
        // therefore need an additive repair when a later application model adds
        // fields. This preserves the existing database (and the SUPER user).
        if (!await HasColumnAsync(dbContext, "OperationalGeneratorReadings", "OilId", cancellationToken))
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"OperationalGeneratorReadings\" ADD COLUMN \"OilId\" INTEGER NULL;",
                cancellationToken);
        if (!await HasColumnAsync(dbContext, "OperationalGeneratorReadings", "OilTransactionId", cancellationToken))
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"OperationalGeneratorReadings\" ADD COLUMN \"OilTransactionId\" INTEGER NULL;",
                cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS \"IX_OperationalGeneratorReadings_OilId\" ON \"OperationalGeneratorReadings\" (\"OilId\");",
            cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS \"IX_OperationalGeneratorReadings_OilTransactionId\" ON \"OperationalGeneratorReadings\" (\"OilTransactionId\");",
            cancellationToken);

        // EnsureCreated does not apply new entities to an existing SQLite file. The
        // commercial capture references include the optional generator/oil setup,
        // so a database created before C6.1 must receive this additive table before
        // that page can be opened. No data is replaced or seeded here.
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "GeneratorMaintenanceOilConfigurations" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_GeneratorMaintenanceOilConfigurations" PRIMARY KEY AUTOINCREMENT,
                "GeneratorId" INTEGER NOT NULL,
                "ServiceItemId" INTEGER NOT NULL,
                "OilId" INTEGER NOT NULL,
                "QuantityLiters" TEXT NOT NULL,
                "IsActive" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "CreatedBy" INTEGER NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "UpdatedBy" INTEGER NOT NULL,
                "DeletedAt" TEXT NULL,
                CONSTRAINT "FK_GeneratorMaintenanceOilConfigurations_Generators_GeneratorId"
                    FOREIGN KEY ("GeneratorId") REFERENCES "Generators" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_GeneratorMaintenanceOilConfigurations_ServiceItems_ServiceItemId"
                    FOREIGN KEY ("ServiceItemId") REFERENCES "ServiceItems" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_GeneratorMaintenanceOilConfigurations_Oils_OilId"
                    FOREIGN KEY ("OilId") REFERENCES "Oils" ("Id") ON DELETE RESTRICT
            );
            """,
            cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS \"UX_GeneratorMaintenanceOilConfigurations_Generator_Item\" " +
            "ON \"GeneratorMaintenanceOilConfigurations\" (\"GeneratorId\", \"ServiceItemId\") WHERE \"DeletedAt\" IS NULL;",
            cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS \"IX_GeneratorMaintenanceOilConfigurations_Oil\" " +
            "ON \"GeneratorMaintenanceOilConfigurations\" (\"OilId\") WHERE \"DeletedAt\" IS NULL;",
            cancellationToken);
    }

    private static async Task<bool> HasColumnAsync(
        FuelTrackDbContext dbContext,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{tableName}\");";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }
}
