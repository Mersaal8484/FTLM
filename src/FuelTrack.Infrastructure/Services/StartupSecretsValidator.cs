using System.Text.RegularExpressions;
using FuelTrack.Application.Common;
using Microsoft.Extensions.Configuration;

namespace FuelTrack.Infrastructure.Services;

public class StartupSecretsValidator : ISecretsValidator
{
    private readonly IConfiguration _configuration;

    public StartupSecretsValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public SecretsValidationResult Validate()
    {
        var result = new SecretsValidationResult();

        ValidateJwtKey(result);
        ValidateDataEncryptionKey(result);
        ValidateConnectionString(result);
        ValidateAdminPassword(result);
        ValidateJwtIssuer(result);
        ValidateJwtAudience(result);
        ValidateCorsOrigins(result);

        return result;
    }

    private void ValidateDataEncryptionKey(SecretsValidationResult result)
    {
        var key = _configuration["DataEncryption:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            key = Environment.GetEnvironmentVariable("DATA_ENCRYPTION_KEY");
        }

        var env = _configuration["ASPNETCORE_ENVIRONMENT"];
        if (string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(key) || key.StartsWith("{") || key.Contains("Replace", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("SECURITY: DataEncryption:Key must be configured with a 32-byte base64 production key.");
            }
        }
    }

    private void ValidateJwtKey(SecretsValidationResult result)
    {
        var key = _configuration["JwtSettings:Key"];

        if (string.IsNullOrWhiteSpace(key))
        {
            result.Errors.Add("JwtSettings:Key is not configured.");
            return;
        }

        if (key.Contains("Replace", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("Secret", StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add("JwtSettings:Key contains placeholder text (Replace/Secret). Set a unique production key.");
        }

        if (key.Length < 32)
        {
            result.Errors.Add($"JwtSettings:Key must be at least 32 characters (current: {key.Length}).");
        }

        if (!Regex.IsMatch(key, @"\d"))
        {
            result.Errors.Add("JwtSettings:Key must contain at least one digit.");
        }

        if (!Regex.IsMatch(key, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
        {
            result.Errors.Add("JwtSettings:Key must contain at least one special character.");
        }
    }

    private void ValidateConnectionString(SecretsValidationResult result)
    {
        var connStr = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connStr))
        {
            result.Errors.Add("ConnectionStrings:DefaultConnection is not configured.");
            return;
        }

        var env = _configuration["ASPNETCORE_ENVIRONMENT"];
        if (string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
        {
            if (connStr.Contains("Trusted_Connection=True", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("SECURITY: Production connection string must not use Trusted_Connection. Use SQL authentication or managed identity.");
            }

            if (connStr.Contains("DevPass", StringComparison.OrdinalIgnoreCase) ||
                connStr.Contains("DevAdmin", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("SECURITY: Production connection string contains development credentials.");
            }
        }
    }

    private void ValidateAdminPassword(SecretsValidationResult result)
    {
        var password = _configuration["AdminSettings:DefaultPassword"];

        if (string.IsNullOrWhiteSpace(password))
        {
            result.Errors.Add("AdminSettings:DefaultPassword is not configured.");
            return;
        }

        if (password.Contains("DevAdmin", StringComparison.OrdinalIgnoreCase) ||
            password.Contains("Admin@123", StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add("SECURITY: AdminSettings:DefaultPassword is using a development default. Set a strong password.");
        }

        if (password.Length < 8)
        {
            result.Errors.Add("AdminSettings:DefaultPassword must be at least 8 characters.");
        }

        if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            result.Warnings.Add("AdminSettings:DefaultPassword should contain at least one uppercase letter.");
        }

        if (!Regex.IsMatch(password, @"\d"))
        {
            result.Warnings.Add("AdminSettings:DefaultPassword should contain at least one digit.");
        }

        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
        {
            result.Warnings.Add("AdminSettings:DefaultPassword should contain at least one special character.");
        }
    }

    private void ValidateJwtIssuer(SecretsValidationResult result)
    {
        var issuer = _configuration["JwtSettings:Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            result.Errors.Add("JwtSettings:Issuer is not configured.");
        }
    }

    private void ValidateJwtAudience(SecretsValidationResult result)
    {
        var audience = _configuration["JwtSettings:Audience"];
        if (string.IsNullOrWhiteSpace(audience))
        {
            result.Errors.Add("JwtSettings:Audience is not configured.");
        }
    }

    private void ValidateCorsOrigins(SecretsValidationResult result)
    {
        var env = _configuration["ASPNETCORE_ENVIRONMENT"];
        if (!string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
            return;

        var origins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins == null || origins.Length == 0)
        {
            result.Errors.Add("SECURITY: Cors:AllowedOrigins must be configured for production.");
            return;
        }

        foreach (var origin in origins)
        {
            if (origin == "*")
            {
                result.Errors.Add("SECURITY: CORS must not use wildcard (*) in production.");
            }
            if (origin.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add($"SECURITY: CORS origin '{origin}' contains localhost — not allowed in production.");
            }
        }
    }
}
