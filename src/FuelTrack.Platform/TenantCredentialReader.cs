using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Platform;

public sealed class TenantCredentialReader(PlatformDbContext db, ITenantCredentialKeyProvider keys)
{
    public async Task<string> GetPasswordAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        var credential = await db.TenantCredentials.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant database credential is unavailable.");
        var key = await keys.GetKeyAsync(credential.KeyId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant credential encryption key is unavailable.");
        try { return TenantCredentialProtector.Decrypt(credential, key); }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
