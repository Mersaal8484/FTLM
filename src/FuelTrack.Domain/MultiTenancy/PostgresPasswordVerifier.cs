using System.Security.Cryptography;
using System.Text;

namespace FuelTrack.Domain.MultiTenancy;

/// <summary>Creates a PostgreSQL SCRAM-SHA-256 verifier without sending plaintext SQL passwords.</summary>
public static class PostgresPasswordVerifier
{
    public static string Create(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        if (password.Length != 64 || password.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new ArgumentException("Password must be 32 random bytes encoded as lowercase hexadecimal.", nameof(password));
        var salt = RandomNumberGenerator.GetBytes(16);
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[]? salted = null;
        byte[]? clientKey = null;
        try
        {
            const int iterations = 4096;
            salted = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, iterations, HashAlgorithmName.SHA256, 32);
            clientKey = HMACSHA256.HashData(salted, "Client Key"u8);
            var storedKey = SHA256.HashData(clientKey);
            var serverKey = HMACSHA256.HashData(salted, "Server Key"u8);
            return $"SCRAM-SHA-256${iterations}:{Convert.ToBase64String(salt)}${Convert.ToBase64String(storedKey)}:{Convert.ToBase64String(serverKey)}";
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            if (salted is not null) CryptographicOperations.ZeroMemory(salted);
            if (clientKey is not null) CryptographicOperations.ZeroMemory(clientKey);
        }
    }
}
