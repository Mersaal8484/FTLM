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

    public AuthService(
        UserManager<User> userManager,
        FuelTrackDbContext context,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TokenResponseDto> LoginAsync(string userName, string password, string? ipAddress)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid credentials");

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
            .FirstOrDefaultAsync(rt => rt.Token == tokenHash);

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
