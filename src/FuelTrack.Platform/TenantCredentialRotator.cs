using FuelTrack.Domain.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FuelTrack.Platform;

/// <summary>
/// Rotates an existing tenant DB credential and its encrypted envelope in the same
/// PostgreSQL transaction. Callers must coordinate application replicas and drain
/// existing sessions; changing a role password does not close active connections.
/// </summary>
public sealed class TenantCredentialRotator(PlatformDbContext db)
{
    public async Task RotateAsync(Guid tenantId, string newPassword, string keyId, byte[] encryptionKey, CancellationToken cancellationToken = default)
    {
        var role = TenantIdentity.SchemaName(tenantId);
        var verifier = PostgresPasswordVerifier.Create(newPassword);
        var envelope = TenantCredentialProtector.Encrypt(tenantId, newPassword, keyId, encryptionKey);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var credential = await db.TenantCredentials
            .FromSqlInterpolated($"SELECT * FROM platform.\"TenantCredentials\" WHERE \"TenantId\" = {tenantId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Tenant credential does not exist.");

        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
        // The role name derives from a GUID and the verifier is generated locally;
        // plaintext passwords never appear in SQL. Protect PostgreSQL statement logs.
        command.CommandText = $"ALTER ROLE \"{role}\" PASSWORD '{verifier}'";
        await command.ExecuteNonQueryAsync(cancellationToken);
        credential.Replace(envelope.KeyId, envelope.Nonce, envelope.Ciphertext, envelope.Tag);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
