using FuelTrack.Domain.MultiTenancy;
using Npgsql;

namespace FuelTrack.Infrastructure.MultiTenancy;

/// <summary>
/// Applies ordered, trusted application-supplied SQL upgrades, never SQL from a
/// request or tenant. Provisioning and upgrades use the same transaction lock.
/// </summary>
public sealed class TenantSchemaMigrator
{
    public async Task UpgradeAsync(string administratorConnection, Guid tenantId, IReadOnlyList<TenantSchemaUpgrade> upgrades, CancellationToken cancellationToken = default)
    {
        var schema = TenantIdentity.SchemaName(tenantId);
        ArgumentNullException.ThrowIfNull(upgrades);
        for (var i = 0; i < upgrades.Count; i++)
        {
            if (upgrades[i].Version != i + 2 || string.IsNullOrWhiteSpace(upgrades[i].Sql))
                throw new ArgumentException("Upgrades must begin at version 2 and be contiguous.", nameof(upgrades));
        }
        await using var resource = typeof(TenantDatabaseBootstrapper).Assembly.GetManifestResourceStream("FuelTrack.Tenants.001_initial_postgresql.sql")
            ?? throw new InvalidOperationException("Initial PostgreSQL tenant migration is missing.");
        using var reader = new StreamReader(resource);
        var baselineChecksum = TenantDatabaseBootstrapper.Checksum(await reader.ReadToEndAsync(cancellationToken));
        await using var connection = new NpgsqlConnection(administratorConnection);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = 180;
        command.CommandText = "SELECT pg_advisory_xact_lock(hashtextextended(@key, 0))";
        command.Parameters.AddWithValue("key", $"fueltrack:tenant:{schema}");
        await command.ExecuteNonQueryAsync(cancellationToken);
        command.Parameters.Clear();
        command.CommandText = $"SELECT \"Version\", \"Checksum\" FROM \"{schema}\".\"__TenantSchemaVersions\" ORDER BY \"Version\"";
        var installed = new List<(int Version, string Checksum)>();
        await using (var rows = await command.ExecuteReaderAsync(cancellationToken))
            while (await rows.ReadAsync(cancellationToken)) installed.Add((rows.GetInt32(0), rows.GetString(1)));
        if (installed.Count == 0 || installed[0] != (1, baselineChecksum) || installed.Count > upgrades.Count + 1)
            throw new InvalidOperationException("Tenant schema version history is missing, unknown, or inconsistent.");
        for (var i = 1; i < installed.Count; i++)
            if (installed[i] != (i + 1, TenantDatabaseBootstrapper.Checksum(upgrades[i - 1].Sql)))
                throw new InvalidOperationException("Tenant schema upgrade history does not match the supplied migrations.");

        command.CommandText = $"SET LOCAL search_path TO \"{schema}\"";
        await command.ExecuteNonQueryAsync(cancellationToken);
        foreach (var upgrade in upgrades.Skip(installed.Count - 1))
        {
            command.CommandText = upgrade.Sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
            command.CommandText = $"INSERT INTO \"{schema}\".\"__TenantSchemaVersions\" (\"Version\", \"Checksum\") VALUES (@version, @checksum)";
            command.Parameters.AddWithValue("version", upgrade.Version);
            command.Parameters.AddWithValue("checksum", TenantDatabaseBootstrapper.Checksum(upgrade.Sql));
            await command.ExecuteNonQueryAsync(cancellationToken);
            command.Parameters.Clear();
        }
        command.CommandText = $"GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA \"{schema}\" TO \"{schema}\"; REVOKE ALL ON \"{schema}\".\"__TenantSchemaVersions\" FROM \"{schema}\"; GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA \"{schema}\" TO \"{schema}\";";
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

public sealed record TenantSchemaUpgrade(int Version, string Sql);
