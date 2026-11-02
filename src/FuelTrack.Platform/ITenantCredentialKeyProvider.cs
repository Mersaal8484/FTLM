namespace FuelTrack.Platform;

/// <summary>
/// Supplies a 32-byte key from an external secret manager. Implementations must
/// return a new caller-owned buffer and must not fall back to another key ID.
/// </summary>
public interface ITenantCredentialKeyProvider
{
    ValueTask<byte[]?> GetKeyAsync(string keyId, CancellationToken cancellationToken = default);
}
