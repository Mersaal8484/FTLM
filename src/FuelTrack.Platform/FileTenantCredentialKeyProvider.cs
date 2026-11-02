using System.Security.Cryptography;

namespace FuelTrack.Platform;

/// <summary>
/// Reads keys from a trusted, externally mounted secrets directory. The deployment
/// must protect the directory and its parent mount against writes by other users.
/// Each key is a base64-encoded 32-byte value in a private regular file.
/// </summary>
public sealed class FileTenantCredentialKeyProvider(string secretsDirectory) : ITenantCredentialKeyProvider
{
    private readonly string _directory = Path.GetFullPath(secretsDirectory);

    public async ValueTask<byte[]?> GetKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        if (keyId.Length > 100 || keyId.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_'))
            throw new ArgumentException("Invalid credential key identifier.", nameof(keyId));
        if (OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Use a secrets provider that validates Windows ACLs.");
        var path = Path.Combine(_directory, keyId + ".key");
        if (!File.Exists(path)) return null;
        var info = new FileInfo(path);
        if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("Secret key file must not be a symbolic link.");
        var permissions = File.GetUnixFileMode(path);
        const UnixFileMode unsafePermissions = UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;
        if ((permissions & unsafePermissions) != 0)
            throw new InvalidOperationException("Secret key file must not be accessible to group or other users.");
        var encoded = await File.ReadAllTextAsync(path, cancellationToken);
        byte[] key;
        try { key = Convert.FromBase64String(encoded.Trim()); }
        catch (FormatException) { throw new CryptographicException("Credential key is not valid base64."); }
        if (key.Length == 32) return key;
        CryptographicOperations.ZeroMemory(key);
        throw new CryptographicException("Credential key must be 32 bytes.");
    }
}
