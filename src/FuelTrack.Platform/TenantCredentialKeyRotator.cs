using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Platform;

/// <summary>Re-encrypts an existing credential under an externally supplied new key.</summary>
public sealed class TenantCredentialKeyRotator(PlatformDbContext db, ITenantCredentialKeyProvider keys)
{
    public async Task RewrapAsync(Guid tenantId, string newKeyId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(newKeyId);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var current = await db.TenantCredentials
            .FromSqlInterpolated($"SELECT * FROM platform.\"TenantCredentials\" WHERE \"TenantId\" = {tenantId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Tenant database credential is unavailable.");
        var oldKey = await keys.GetKeyAsync(current.KeyId, cancellationToken)
            ?? throw new InvalidOperationException("Current encryption key is unavailable.");
        try
        {
            var newKey = await keys.GetKeyAsync(newKeyId, cancellationToken)
                ?? throw new InvalidOperationException("New encryption key is unavailable.");
            try
            {
                var plaintext = TenantCredentialProtector.Decrypt(current, oldKey);
                var replacement = TenantCredentialProtector.Encrypt(tenantId, plaintext, newKeyId, newKey);
                current.Replace(replacement.KeyId, replacement.Nonce, replacement.Ciphertext, replacement.Tag);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            finally { CryptographicOperations.ZeroMemory(newKey); }
        }
        finally { CryptographicOperations.ZeroMemory(oldKey); }
    }
}
