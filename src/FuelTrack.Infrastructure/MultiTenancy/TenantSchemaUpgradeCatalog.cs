using FuelTrack.Domain.MultiTenancy;
using Npgsql;

namespace FuelTrack.Infrastructure.MultiTenancy;

/// <summary>Trusted PostgreSQL tenant upgrades. Platform orchestration passes this immutable list to TenantSchemaMigrator.</summary>
public static class TenantSchemaUpgradeCatalog
{
    public static async Task<IReadOnlyList<TenantSchemaUpgrade>> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return [
            new TenantSchemaUpgrade(2, await ReadUpgradeAsync("002_bitbus_operational_capture", cancellationToken)),
            new TenantSchemaUpgrade(3, await ReadUpgradeAsync("003_phase3_review_columns", cancellationToken)),
            new TenantSchemaUpgrade(4, await ReadUpgradeAsync("004_phase3b_fuel_load", cancellationToken)),
            new TenantSchemaUpgrade(5, await ReadUpgradeAsync("005_phase3b_operational_indicators", cancellationToken)),
            new TenantSchemaUpgrade(6, await ReadUpgradeAsync("006_phase3b_patch", cancellationToken)),
            new TenantSchemaUpgrade(7, await ReadUpgradeAsync("007_phase3b_1_corrections", cancellationToken)),
            new TenantSchemaUpgrade(8, await ReadUpgradeAsync("008_phase4_service_intervals", cancellationToken))
        ];
    }

    /// <summary>Used during provisioning in the caller-owned Platform transaction.</summary>
    public static async Task ApplyCurrentWithinTransactionAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var schema = TenantIdentity.SchemaName(tenantId);
        var upgrades = await GetCurrentAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = 180;
        command.CommandText = $"SET LOCAL search_path TO \"{schema}\"";
        await command.ExecuteNonQueryAsync(cancellationToken);
        foreach (var upgrade in upgrades)
        {
            command.CommandText = upgrade.Sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
            command.CommandText = $"INSERT INTO \"{schema}\".\"__TenantSchemaVersions\" (\"Version\", \"Checksum\") VALUES (@version, @checksum)";
            command.Parameters.AddWithValue("version", upgrade.Version);
            command.Parameters.AddWithValue("checksum", TenantDatabaseBootstrapper.Checksum(upgrade.Sql));
            await command.ExecuteNonQueryAsync(cancellationToken);
            command.Parameters.Clear();
        }
    }

    private static async Task<string> ReadUpgradeAsync(string name, CancellationToken cancellationToken)
    {
        await using var stream = typeof(TenantSchemaUpgradeCatalog).Assembly.GetManifestResourceStream($"FuelTrack.Tenants.{name}.sql")
            ?? throw new InvalidOperationException($"PostgreSQL tenant upgrade {name} is missing.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
