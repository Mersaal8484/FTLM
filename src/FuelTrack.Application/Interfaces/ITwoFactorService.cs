namespace FuelTrack.Application.Interfaces;

public interface ITwoFactorService
{
    string GenerateSecretKey();
    string GetQrCodeUri(string secretKey, string email, string issuer);
    bool VerifyCode(string secretKey, string code);
    string[] GenerateBackupCodes();
    bool VerifyBackupCode(string backupCodesJson, string code);
}
