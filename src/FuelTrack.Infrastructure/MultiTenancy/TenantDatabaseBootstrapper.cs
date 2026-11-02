using FuelTrack.Domain.MultiTenancy;
using Npgsql;
using System.Security.Cryptography;
using System.Text;

namespace FuelTrack.Infrastructure.MultiTenancy;

/// <summary>
/// Experimental initial-schema bootstrap. The caller must keep the generated
/// tenant credential in a secret store and must not activate the tenant until the
/// identity, subscription and migration workflows have completed.
/// </summary>
public sealed class TenantDatabaseBootstrapper
{
    public async Task CreateAsync(string administratorConnection, Guid tenantId, string tenantPassword, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(administratorConnection);
        await using var connection = new NpgsqlConnection(administratorConnection);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await CreateWithinTransactionAsync(connection, transaction, tenantId, tenantPassword, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>Runs using a caller-owned administrator transaction; does not commit.</summary>
    public async Task CreateWithinTransactionAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid tenantId, string tenantPassword, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        if (transaction.Connection != connection) throw new ArgumentException("Transaction and connection must match.", nameof(transaction));
        var schema = TenantIdentity.SchemaName(tenantId);
        var passwordVerifier = PostgresPasswordVerifier.Create(tenantPassword);

        await using var resource = typeof(TenantDatabaseBootstrapper).Assembly.GetManifestResourceStream("FuelTrack.Tenants.001_initial_postgresql.sql")
            ?? throw new InvalidOperationException("Initial PostgreSQL tenant migration is missing.");
        using var reader = new StreamReader(resource);
        var initialSchema = await reader.ReadToEndAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = 180;
        command.CommandText = "SELECT pg_advisory_xact_lock(hashtextextended(@schema, 0))";
        command.Parameters.AddWithValue("schema", $"fueltrack:tenant:{schema}");
        await command.ExecuteNonQueryAsync(cancellationToken);
        command.Parameters.Clear();

        // The SCRAM verifier contains only the algorithm marker, digits and base64 characters.
        // PostgreSQL cannot parameterize CREATE ROLE; never put the plaintext password in SQL.
        command.CommandText = $"CREATE ROLE \"{schema}\" LOGIN PASSWORD '{passwordVerifier}'; CREATE SCHEMA \"{schema}\"; REVOKE ALL ON SCHEMA \"{schema}\" FROM PUBLIC; SET LOCAL search_path TO \"{schema}\";";
        await command.ExecuteNonQueryAsync(cancellationToken);
        command.CommandText = initialSchema;
        await command.ExecuteNonQueryAsync(cancellationToken);
        command.CommandText = $"""
            CREATE TABLE "{schema}"."__TenantSchemaVersions" ("Version" integer PRIMARY KEY, "Checksum" character(64) NOT NULL, "AppliedAt" timestamp with time zone NOT NULL DEFAULT now());
            INSERT INTO "{schema}"."__TenantSchemaVersions" ("Version", "Checksum") VALUES (1, '{Checksum(initialSchema)}');
            GRANT USAGE ON SCHEMA "{schema}" TO "{schema}";
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA "{schema}" TO "{schema}";
            REVOKE ALL ON "{schema}"."__TenantSchemaVersions" FROM "{schema}";
            GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA "{schema}" TO "{schema}";
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal static string Checksum(string sql) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql))).ToLowerInvariant();
}
