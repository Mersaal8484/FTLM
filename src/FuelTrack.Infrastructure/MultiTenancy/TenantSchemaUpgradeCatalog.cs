using FuelTrack.Domain.MultiTenancy;
using Npgsql;

namespace FuelTrack.Infrastructure.MultiTenancy;

/// <summary>Trusted PostgreSQL tenant upgrades. Platform orchestration passes this immutable list to TenantSchemaMigrator.</summary>
public static class TenantSchemaUpgradeCatalog
{
    public static async Task<IReadOnlyList<TenantSchemaUpgrade>> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = typeof(TenantSchemaUpgradeCatalog).Assembly.GetManifestResourceStream("FuelTrack.Tenants.002_bitbus_operational_capture.sql")
            ?? throw new InvalidOperationException("Bitbus PostgreSQL tenant upgrade is missing.");
        using var reader = new StreamReader(stream);
        return [new TenantSchemaUpgrade(2, await reader.ReadToEndAsync(cancellationToken))];
    }

    /// <summary>Used during provisioning in the caller-owned Platform transaction.</summary>
    public static async Task ApplyCurrentWithinTransactionAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var schema = TenantIdentity.SchemaName(tenantId);
        var upgrade = (await GetCurrentAsync(cancellationToken))[0];
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = 180;
        command.CommandText = $"SET LOCAL search_path TO \"{schema}\"";
        await command.ExecuteNonQueryAsync(cancellationToken);
        command.CommandText = upgrade.Sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
        command.CommandText = $"INSERT INTO \"{schema}\".\"__TenantSchemaVersions\" (\"Version\", \"Checksum\") VALUES (2, @checksum)";
        command.Parameters.AddWithValue("checksum", TenantDatabaseBootstrapper.Checksum(upgrade.Sql));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
