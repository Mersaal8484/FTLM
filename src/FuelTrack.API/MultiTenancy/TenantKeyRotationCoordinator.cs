using System.Security.Cryptography;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.MultiTenancy;
using FuelTrack.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FuelTrack.API.MultiTenancy;

/// <summary>Offline maintenance only: rotate credentials and all TOTP secrets in one transaction.</summary>
public sealed class TenantKeyRotationCoordinator(PlatformDbContext platform, ITenantRequestContext tenant, ITenantCredentialKeyProvider keys)
{
    public async Task<int> RotateAsync(Guid tenantId, string newKeyId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || tenant.TenantId != tenantId) throw new InvalidOperationException("Trusted tenant context required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(newKeyId);
        await using var transaction = await platform.Database.BeginTransactionAsync(cancellationToken);
        var entry = await platform.Tenants.FromSqlInterpolated($"SELECT * FROM platform.\"Tenants\" WHERE \"Id\" = {tenantId} FOR UPDATE")
            .SingleAsync(cancellationToken);
        if (entry.Status is not (PlatformTenantStatus.Suspended or PlatformTenantStatus.Provisioning))
            throw new InvalidOperationException("Tenant must be suspended before offline rotation.");
        var credential = await platform.TenantCredentials.FromSqlInterpolated($"SELECT * FROM platform.\"TenantCredentials\" WHERE \"TenantId\" = {tenantId} FOR UPDATE")
            .SingleAsync(cancellationToken);
        var oldKey = await keys.GetKeyAsync(credential.KeyId, cancellationToken) ?? throw new InvalidOperationException("Current encryption key is unavailable.");
        try
        {
            var newKey = await keys.GetKeyAsync(newKeyId, cancellationToken) ?? throw new InvalidOperationException("New encryption key is unavailable.");
            try
            {
                var password = TenantCredentialProtector.Decrypt(credential, oldKey);
                var replacement = TenantCredentialProtector.Encrypt(tenantId, password, newKeyId, newKey);
                var crypto = new TenantTwoFactorSecretProtector(tenant, platform, keys);
                var connection = (NpgsqlConnection)platform.Database.GetDbConnection();
                var dbTransaction = (NpgsqlTransaction)transaction.GetDbTransaction();
                var schema = TenantIdentity.SchemaName(tenantId);
                var users = new List<(int Id, string Secret)>();
                await using (var read = new NpgsqlCommand($"SELECT \"Id\", \"TwoFactorSecret\" FROM \"{schema}\".\"Users\" WHERE \"TwoFactorSecret\" IS NOT NULL FOR UPDATE", connection, dbTransaction))
                await using (var reader = await read.ExecuteReaderAsync(cancellationToken))
                    while (await reader.ReadAsync(cancellationToken)) users.Add((reader.GetInt32(0), reader.GetString(1)));
                foreach (var (userId, encrypted) in users)
                {
                    var plain = await crypto.UnprotectAsync(userId, encrypted, cancellationToken);
                    var wrapped = await crypto.ProtectWithKeyAsync(userId, plain, newKeyId, cancellationToken);
                    await using var update = new NpgsqlCommand($"UPDATE \"{schema}\".\"Users\" SET \"TwoFactorSecret\" = @secret, \"ConcurrencyStamp\" = @stamp WHERE \"Id\" = @id", connection, dbTransaction);
                    update.Parameters.AddWithValue("secret", wrapped);
                    update.Parameters.AddWithValue("stamp", Guid.NewGuid().ToString());
                    update.Parameters.AddWithValue("id", userId);
                    if (await update.ExecuteNonQueryAsync(cancellationToken) != 1) throw new InvalidOperationException("Two-factor account changed during rotation.");
                }
                credential.Replace(replacement.KeyId, replacement.Nonce, replacement.Ciphertext, replacement.Tag);
                await platform.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return users.Count;
            }
            finally { CryptographicOperations.ZeroMemory(newKey); }
        }
        finally { CryptographicOperations.ZeroMemory(oldKey); }
    }
}
