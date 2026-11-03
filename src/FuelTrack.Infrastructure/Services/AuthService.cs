using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FuelTrack.Application.DTOs.Auth;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace FuelTrack.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly FuelTrackDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantRequestContext? _tenant;
    private readonly ITwoFactorService? _twoFactor;
    private readonly ITwoFactorSecretProtector? _twoFactorProtector;
    private const string TwoFactorMarker = "two-factor-challenge-v1";
    private const string UsedTotpMarker = "two-factor-used-totp-v1";

    public AuthService(
        UserManager<User> userManager,
        FuelTrackDbContext context,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ITenantRequestContext? tenant = null,
        ITwoFactorService? twoFactor = null,
        ITwoFactorSecretProtector? twoFactorProtector = null)
    {
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _tenant = tenant;
        _twoFactor = twoFactor;
        _twoFactorProtector = twoFactorProtector;
    }

    public async Task<TokenResponseDto> LoginAsync(string userName, string password, string? ipAddress)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid credentials");

        if (user.TwoFactorEnabled)
            throw new UnauthorizedAccessException("Two-factor authentication is required");

        if (await _userManager.IsLockedOutAsync(user))
            throw new UnauthorizedAccessException("Account is locked due to multiple failed attempts. Try again later.");

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);

            _context.LoginAttempts.Add(new LoginAttempt
            {
                UserId = user.Id,
                IsSuccessful = false,
                AttemptedAt = DateTime.UtcNow,
                IpAddress = ipAddress
            });
            await _context.SaveChangesAsync();
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        return await IssueAuthenticatedTokenAsync(user, ipAddress);
    }

    public async Task<string> BeginTwoFactorAsync(string userName, string password)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null || !user.IsActive || !user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret) ||
            string.IsNullOrEmpty(user.SecurityStamp) || await _userManager.IsLockedOutAsync(user))
            throw new UnauthorizedAccessException("Invalid credentials");
        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            await _userManager.AccessFailedAsync(user);
            throw new UnauthorizedAccessException("Invalid credentials");
        }
        var challenge = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = HashToken("two-factor:" + challenge),
            DeviceInfo = TwoFactorMarker,
            ReplacedByToken = user.SecurityStamp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
        });
        await _context.SaveChangesAsync();
        return challenge;
    }

    public async Task<TokenResponseDto> CompleteTwoFactorAsync(string challenge, string code, string? ipAddress)
    {
        if (challenge is null || challenge.Length != 64 || challenge.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')) || _twoFactor is null)
            throw new UnauthorizedAccessException("Invalid two-factor challenge");
        var hash = HashToken("two-factor:" + challenge);
        var now = DateTime.UtcNow;
        var item = await _context.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(t => t.Token == hash && t.DeviceInfo == TwoFactorMarker);
        if (item is null || item.IsRevoked || item.ExpiresAt <= now)
            throw new UnauthorizedAccessException("Invalid two-factor challenge");
        // Exactly one attempt can consume a challenge, including an incorrect code.
        var consumed = await _context.RefreshTokens.Where(t => t.Id == item.Id && t.DeviceInfo == TwoFactorMarker && !t.IsRevoked && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true).SetProperty(t => t.RevokedAt, now));
        if (consumed != 1) throw new UnauthorizedAccessException("Invalid two-factor challenge");
        var user = await _userManager.FindByIdAsync(item.UserId.ToString());
        if (user is null || !user.IsActive || !user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret) ||
            user.SecurityStamp != item.ReplacedByToken || await _userManager.IsLockedOutAsync(user))
            throw new UnauthorizedAccessException("Invalid two-factor code");
        var secret = _configuration.GetValue<bool>("MultiTenancy:Enabled")
            ? await (_twoFactorProtector ?? throw new InvalidOperationException("Two-factor secret protection is required.")).UnprotectAsync(user.Id, user.TwoFactorSecret)
            : user.TwoFactorSecret;
        var multiTenant = _configuration.GetValue<bool>("MultiTenancy:Enabled");
        var isTotp = _twoFactor.VerifyCode(secret, code);
        var isRecovery = multiTenant && FuelTrack.Application.Services.RecoveryCodes.Consume(user.TwoFactorBackupCodes, code) is not null;
        if (!isTotp && !isRecovery)
        {
            await _userManager.AccessFailedAsync(user);
            throw new UnauthorizedAccessException("Invalid two-factor code");
        }
        if (multiTenant)
        {
            if (!_context.Database.IsNpgsql() || _tenant?.TenantId is not Guid tenantId || tenantId == Guid.Empty)
                throw new InvalidOperationException("A trusted PostgreSQL tenant is required for two-factor authentication.");
            // A one-use challenge alone does not stop the same TOTP being submitted
            // through two separate challenges. Serialize checks per tenant and user,
            // and record only a hash until the accepted TOTP window has elapsed.
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var lockKey = $"fueltrack:totp:{tenantId:N}:{user.Id}";
            await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))");
            var currentUser = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == user.Id);
            if (currentUser is null || !currentUser.IsActive || !currentUser.TwoFactorEnabled ||
                currentUser.SecurityStamp != item.ReplacedByToken || currentUser.TwoFactorSecret != user.TwoFactorSecret ||
                await _userManager.IsLockedOutAsync(currentUser))
                throw new UnauthorizedAccessException("Invalid two-factor code");
            if (isRecovery)
            {
                var remaining = FuelTrack.Application.Services.RecoveryCodes.Consume(currentUser.TwoFactorBackupCodes, code);
                if (remaining is null) throw new UnauthorizedAccessException("Invalid two-factor code");
                var stamp = Guid.NewGuid().ToString();
                var changed = await _context.Users.Where(u => u.Id == user.Id && u.TwoFactorBackupCodes == currentUser.TwoFactorBackupCodes &&
                    u.ConcurrencyStamp == currentUser.ConcurrencyStamp && u.SecurityStamp == currentUser.SecurityStamp && u.TwoFactorEnabled && u.IsActive)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.TwoFactorBackupCodes, remaining).SetProperty(u => u.ConcurrencyStamp, stamp));
                if (changed != 1) throw new UnauthorizedAccessException("Invalid two-factor code");
                await _context.Entry(user).ReloadAsync();
            }
            else
            {
                if (!_twoFactor.VerifyCode(secret, code)) throw new UnauthorizedAccessException("Invalid two-factor code");
                var codeHash = HashToken($"two-factor-used:{user.Id}:{code}");
                var checkedAt = DateTime.UtcNow;
                if (await _context.RefreshTokens.AnyAsync(t => t.UserId == user.Id && t.DeviceInfo == UsedTotpMarker &&
                    t.Token == codeHash && t.ExpiresAt > checkedAt))
                    throw new UnauthorizedAccessException("Invalid two-factor code");
                _context.RefreshTokens.Add(new RefreshToken
                {
                    UserId = user.Id, Token = codeHash, DeviceInfo = UsedTotpMarker,
                    ExpiresAt = checkedAt.AddMinutes(2), IsRevoked = true, RevokedAt = checkedAt
                });
                await _context.SaveChangesAsync();
            }
            await transaction.CommitAsync();
        }
        return await IssueAuthenticatedTokenAsync(user, ipAddress);
    }

    private async Task<TokenResponseDto> IssueAuthenticatedTokenAsync(User user, string? ipAddress)
    {
        await _userManager.ResetAccessFailedCountAsync(user);

        _context.LoginAttempts.Add(new LoginAttempt
        {
            UserId = user.Id,
            IsSuccessful = true,
            AttemptedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        });

        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = user.Id;

        var (token, expiresAt) = await GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();
        await SaveRefreshToken(user.Id, refreshToken, expiresAt);

        await _context.SaveChangesAsync();

        SetRefreshTokenCookie(refreshToken);

        return new TokenResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    public async Task<TokenResponseDto> RefreshAsync()
    {
        var refreshToken = _httpContextAccessor.HttpContext?.Request.Cookies["refresh_token"];
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedAccessException("Refresh token required");

        var tokenHash = HashToken(refreshToken);
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == tokenHash && rt.DeviceInfo != TwoFactorMarker);

        if (storedToken == null)
            throw new UnauthorizedAccessException("Invalid refresh token");

        if (storedToken.IsRevoked)
        {
            // Token reuse attack detected — revoke ALL active tokens for this user
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == storedToken.UserId && !rt.IsRevoked)
                .ToListAsync();
            activeTokens.ForEach(t => t.IsRevoked = true);
            await _context.SaveChangesAsync();
            throw new UnauthorizedAccessException("Refresh token has been revoked");
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired");

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive");

        var (newToken, expiresAt) = await GenerateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken();
        var newTokenHash = HashToken(newRefreshToken);
        storedToken.ReplacedByToken = newTokenHash;
        await SaveRefreshToken(user.Id, newRefreshToken, expiresAt);

        await _context.SaveChangesAsync();

        SetRefreshTokenCookie(newRefreshToken);

        return new TokenResponseDto
        {
            Token = newToken,
            ExpiresAt = expiresAt
        };
    }

    public async Task RevokeTokensAsync(int userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        tokens.ForEach(t => t.IsRevoked = true);
        await _context.SaveChangesAsync();
    }

    public async Task<SignalRTokenResponseDto> GenerateSignalRTokenAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new UnauthorizedAccessException("User not found");

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = jwtSettings["Key"] ?? throw new InvalidOperationException("JwtSettings:Key is not configured");
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var expirationMinutes = jwtSettings["SignalRAccessTokenExpirationMinutes"] ?? "10";
        var expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(expirationMinutes));
        var sessionId = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName!),
            new("sessionId", sessionId),
            new("tokenType", "signalr")
        };
        AddTenantClaim(claims);

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"] ?? "FuelTrack.API",
            audience: jwtSettings["SignalRAudience"] ?? "FuelTrack.SignalR",
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256)
        );

        return new SignalRTokenResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            SessionId = sessionId,
            ExpiresAt = expiresAt
        };
    }

    private async Task<(string token, DateTime expiresAt)> GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = jwtSettings["Key"] ?? throw new InvalidOperationException("JwtSettings:Key is not configured");
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var expirationMinutes = jwtSettings["AccessTokenExpirationMinutes"] ?? "60";
        var expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(expirationMinutes));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email ?? ""),
            new("fullName", user.FullName)
        };
        AddTenantClaim(claims);

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"] ?? "FuelTrack.API",
            audience: jwtSettings["Audience"] ?? "FuelTrack.Clients",
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256)
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private void AddTenantClaim(List<Claim> claims)
    {
        if (!_configuration.GetValue<bool>("MultiTenancy:Enabled")) return;
        if (_tenant?.TenantId is not Guid tenantId || tenantId == Guid.Empty)
            throw new InvalidOperationException("A trusted tenant context is required to issue a token.");
        claims.Add(new Claim("tenant_id", tenantId.ToString("D")));
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7),
            Path = "/api/auth"
        };
        httpContext.Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);
    }

    private async Task SaveRefreshToken(int userId, string token, DateTime expiresAt)
    {
        var tokenHash = HashToken(token);
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Token = tokenHash,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
