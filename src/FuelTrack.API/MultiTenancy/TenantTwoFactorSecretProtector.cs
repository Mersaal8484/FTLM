using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FuelTrack.Application.Interfaces;
using FuelTrack.Platform;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.MultiTenancy;

/// <summary>Protects tenant TOTP secrets with externally stored, versioned keys.</summary>
public sealed class TenantTwoFactorSecretProtector(
    ITenantRequestContext tenant,
    PlatformDbContext platform,
    ITenantCredentialKeyProvider keys) : ITwoFactorSecretProtector
{
    private const string Prefix = "ft2fa1:";
    private sealed record Envelope(string KeyId, byte[] Nonce, byte[] Ciphertext, byte[] Tag);
    private Guid TenantId => tenant.TenantId is Guid id && id != Guid.Empty
        ? id : throw new InvalidOperationException("A trusted tenant is required.");

    public async Task<string> ProtectAsync(int userId, string secret, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(secret)) throw new ArgumentException("Invalid two-factor secret.");
        var tenantId = TenantId;
        var keyId = await platform.TenantCredentials.AsNoTracking().Where(x => x.TenantId == tenantId)
            .Select(x => x.KeyId).SingleAsync(cancellationToken);
        return await ProtectWithKeyAsync(userId, secret, keyId, cancellationToken);
    }

    internal async Task<string> ProtectWithKeyAsync(int userId, string secret, string keyId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(secret)) throw new ArgumentException("Invalid two-factor secret.");
        var tenantId = TenantId;
        var key = await keys.GetKeyAsync(keyId, cancellationToken) ?? throw new InvalidOperationException("Two-factor encryption key is unavailable.");
        var plain = Encoding.UTF8.GetBytes(secret);
        try
        {
            var nonce = RandomNumberGenerator.GetBytes(12);
            var cipher = new byte[plain.Length];
            var tag = new byte[16];
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plain, cipher, tag, AssociatedData(tenantId, userId, keyId));
            return Prefix + Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new Envelope(keyId, nonce, cipher, tag)));
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plain); }
    }

    public async Task<string> UnprotectAsync(int userId, string value, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || value is null || !value.StartsWith(Prefix, StringComparison.Ordinal) || value.Length > 4096)
            throw new CryptographicException("Invalid two-factor secret envelope.");
        Envelope envelope;
        try { envelope = JsonSerializer.Deserialize<Envelope>(Convert.FromBase64String(value[Prefix.Length..])) ?? throw new CryptographicException("Invalid two-factor secret envelope."); }
        catch (Exception ex) when (ex is FormatException or JsonException) { throw new CryptographicException("Invalid two-factor secret envelope.", ex); }
        if (string.IsNullOrWhiteSpace(envelope.KeyId) || envelope.Nonce?.Length != 12 || envelope.Tag?.Length != 16 ||
            envelope.Ciphertext is null || envelope.Ciphertext.Length is 0 or > 256)
            throw new CryptographicException("Invalid two-factor secret envelope.");
        var tenantId = TenantId;
        var key = await keys.GetKeyAsync(envelope.KeyId, cancellationToken) ?? throw new InvalidOperationException("Two-factor encryption key is unavailable.");
        var plain = new byte[envelope.Ciphertext.Length];
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(envelope.Nonce, envelope.Ciphertext, envelope.Tag, plain, AssociatedData(tenantId, userId, envelope.KeyId));
            return Encoding.UTF8.GetString(plain);
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plain); }
    }

    private static byte[] AssociatedData(Guid tenantId, int userId, string keyId) =>
        Encoding.UTF8.GetBytes($"fueltrack:two-factor:v1:{tenantId:N}:{userId}:{keyId}");
}
