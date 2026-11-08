using System.Security.Cryptography;
using System.Text;

namespace FuelTrack.Platform;

public static class TenantCredentialProtector
{
    public static TenantCredential Encrypt(Guid tenantId, string password, string keyId, byte[] key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ValidateKey(key);
        var plaintext = Encoding.UTF8.GetBytes(password);
        try
        {
            var nonce = RandomNumberGenerator.GetBytes(12);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, AssociatedData(tenantId, keyId));
            return new TenantCredential(tenantId, keyId, nonce, ciphertext, tag);
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    public static string Decrypt(TenantCredential credential, byte[] key)
    {
        ValidateKey(key);
        var plaintext = new byte[credential.Ciphertext.Length];
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(credential.Nonce, credential.Ciphertext, credential.Tag, plaintext, AssociatedData(credential.TenantId, credential.KeyId));
            return Encoding.UTF8.GetString(plaintext);
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    private static byte[] AssociatedData(Guid tenantId, string keyId) => Encoding.UTF8.GetBytes($"fueltrack-tenant-credential:v1:{tenantId:N}:{keyId}");
    private static void ValidateKey(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != 32) throw new ArgumentException("Credential encryption key must be 32 bytes.", nameof(key));
    }
}
