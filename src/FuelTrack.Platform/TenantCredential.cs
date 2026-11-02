namespace FuelTrack.Platform;

/// <summary>Ciphertext only; encryption keys are supplied by an external key provider.</summary>
public sealed class TenantCredential
{
    private TenantCredential() { }
    public TenantCredential(Guid tenantId, string keyId, byte[] nonce, byte[] ciphertext, byte[] tag)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        if (keyId.Length > 100 || nonce.Length != 12 || tag.Length != 16 || ciphertext.Length == 0)
            throw new ArgumentException("Invalid credential envelope.");
        TenantId = tenantId;
        Replace(keyId, nonce, ciphertext, tag);
    }
    public Guid TenantId { get; private set; }
    public string KeyId { get; private set; } = "";
    public byte[] Nonce { get; private set; } = [];
    public byte[] Ciphertext { get; private set; } = [];
    public byte[] Tag { get; private set; } = [];
    public DateTimeOffset UpdatedAt { get; private set; }
    public void Replace(string keyId, byte[] nonce, byte[] ciphertext, byte[] tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        if (keyId.Length > 100 || nonce.Length != 12 || tag.Length != 16 || ciphertext.Length == 0)
            throw new ArgumentException("Invalid credential envelope.");
        KeyId = keyId;
        Nonce = nonce.ToArray();
        Ciphertext = ciphertext.ToArray();
        Tag = tag.ToArray();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
