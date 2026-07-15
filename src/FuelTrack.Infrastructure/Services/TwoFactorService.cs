using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FuelTrack.Application.Interfaces;
using OtpNet;

namespace FuelTrack.Infrastructure.Services;

public class TwoFactorService : ITwoFactorService
{
    public string GenerateSecretKey()
    {
        var bytes = new byte[20];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base32Encoding.ToString(bytes);
    }

    public string GetQrCodeUri(string secretKey, string email, string issuer)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(email);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secretKey}&issuer={encodedIssuer}&digits=6&period=30";
    }

    public bool VerifyCode(string secretKey, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            return false;

        var key = Base32Encoding.ToBytes(secretKey);
        var totp = new Totp(key, step: 30, totpSize: 6);
        return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
    }

    public string[] GenerateBackupCodes()
    {
        var codes = new string[10];
        for (var i = 0; i < 10; i++)
        {
            var bytes = new byte[4];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            codes[i] = BitConverter.ToString(bytes).Replace("-", "").Substring(0, 8).ToLowerInvariant();
        }
        return codes;
    }

    public bool VerifyBackupCode(string backupCodesJson, string code)
    {
        if (string.IsNullOrWhiteSpace(backupCodesJson))
            return false;

        var codes = JsonSerializer.Deserialize<string[]>(backupCodesJson);
        if (codes == null)
            return false;

        var normalizedCode = code.ToLowerInvariant().Trim();
        var index = Array.FindIndex(codes, c =>
            c != null && c.ToLowerInvariant() == normalizedCode);

        if (index < 0)
            return false;

        codes[index] = "";
        return true;
    }
}
