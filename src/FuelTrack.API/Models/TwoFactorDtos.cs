using System.ComponentModel.DataAnnotations;

namespace FuelTrack.API.Models;

public class TwoFactorSetupResponse
{
    public string SecretKey { get; set; } = string.Empty;
    public string QrCodeUri { get; set; } = string.Empty;
    public string[] BackupCodes { get; set; } = Array.Empty<string>();
}

public class TwoFactorVerifyRequest
{
    [Required] public string Code { get; set; } = string.Empty;
    public string? TempToken { get; set; }
}

public class TwoFactorEnableRequest
{
    [Required] public string Code { get; set; } = string.Empty;
}

public class TwoFactorDisableRequest
{
    [Required] public string Code { get; set; } = string.Empty;
}

public class TwoFactorLoginChallengeResponse
{
    public bool RequiresTwoFactor { get; set; } = true;
    public string? TempToken { get; set; }
    public string Message { get; set; } = "Two-factor authentication required";
}
