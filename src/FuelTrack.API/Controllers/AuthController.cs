using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;
using FuelTrack.API.Models;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FuelTrack.Infrastructure;
using FuelTrack.Application.DTOs.Auth;
using FuelTrack.Application.Common;

namespace FuelTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IAuthService _authService;
    private readonly FuelTrackDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ITwoFactorService _twoFactorService;

    public AuthController(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IAuthService authService,
        FuelTrackDbContext context,
        IEmailService emailService,
        IConfiguration configuration,
        ITwoFactorService twoFactorService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _authService = authService;
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _twoFactorService = twoFactorService;
    }

    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new User
        {
            UserName = request.UserName,
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 0
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        user.CreatedBy = user.Id;
        await _userManager.UpdateAsync(user);

        await _userManager.AddToRoleAsync(user, "Operator");

        return Ok(new { Message = "User registered successfully", UserId = user.Id });
    }

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null || !user.IsActive)
                return Unauthorized(new { msg = "Invalid credentials" });

            if (user.TwoFactorEnabled)
            {
                return Ok(new TwoFactorLoginChallengeResponse
                {
                    RequiresTwoFactor = true,
                    TempToken = request.UserName,
                    Message = "Two-factor authentication required. Please provide your TOTP code."
                });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _authService.LoginAsync(request.UserName, request.Password, ipAddress);
            SetTokenCookie(response.Token);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { msg = ex.Message });
        }
    }

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login/2fa")]
    public async Task<IActionResult> LoginWithTwoFactor([FromBody] TwoFactorVerifyRequest request)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(request.TempToken);
            if (user == null || !user.IsActive || !user.TwoFactorEnabled)
                return Unauthorized(new { msg = "Invalid request" });

            if (string.IsNullOrEmpty(user.TwoFactorSecret))
                return Unauthorized(new { msg = "2FA not properly configured" });

            if (!_twoFactorService.VerifyCode(user.TwoFactorSecret, request.Code))
            {
                if (!string.IsNullOrEmpty(user.TwoFactorBackupCodes) &&
                    _twoFactorService.VerifyBackupCode(user.TwoFactorBackupCodes, request.Code))
                {
                    await _userManager.UpdateAsync(user);
                }
                else
                {
                    return Unauthorized(new { msg = "Invalid 2FA code" });
                }
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _authService.LoginAsync(user.UserName!, "", ipAddress);
            SetTokenCookie(response.Token);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { msg = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("2fa/setup")]
    public async Task<IActionResult> SetupTwoFactor()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var secretKey = _twoFactorService.GenerateSecretKey();
        user.TwoFactorSecret = secretKey;

        var email = user.Email ?? user.UserName ?? "user";
        var issuer = _configuration["TwoFactor:Issuer"] ?? "FuelTrack";
        var qrCodeUri = _twoFactorService.GetQrCodeUri(secretKey, email, issuer);

        var backupCodes = _twoFactorService.GenerateBackupCodes();
        user.TwoFactorBackupCodes = JsonSerializer.Serialize(backupCodes);

        await _userManager.UpdateAsync(user);

        return Ok(new TwoFactorSetupResponse
        {
            SecretKey = secretKey,
            QrCodeUri = qrCodeUri,
            BackupCodes = backupCodes
        });
    }

    [Authorize]
    [HttpPost("2fa/verify")]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorVerifyRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return BadRequest(new { msg = "2FA setup required. Call /api/auth/2fa/setup first." });

        var isValid = _twoFactorService.VerifyCode(user.TwoFactorSecret, request.Code);
        return Ok(new { valid = isValid });
    }

    [Authorize]
    [HttpPost("2fa/enable")]
    public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorEnableRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return BadRequest(new { msg = "2FA setup required. Call /api/auth/2fa/setup first." });

        if (!_twoFactorService.VerifyCode(user.TwoFactorSecret, request.Code))
            return BadRequest(new { msg = "Invalid 2FA code" });

        user.TwoFactorEnabled = true;
        await _userManager.UpdateAsync(user);

        return Ok(new { msg = "Two-factor authentication enabled" });
    }

    [Authorize]
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorDisableRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (!user.TwoFactorEnabled)
            return BadRequest(new { msg = "2FA is not enabled" });

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return BadRequest(new { msg = "2FA secret not found" });

        if (!_twoFactorService.VerifyCode(user.TwoFactorSecret, request.Code))
            return BadRequest(new { msg = "Invalid 2FA code" });

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.TwoFactorBackupCodes = null;
        await _userManager.UpdateAsync(user);

        return Ok(new { msg = "Two-factor authentication disabled" });
    }

    [Authorize]
    [HttpPost("2fa/backup-codes")]
    public async Task<IActionResult> RegenerateBackupCodes([FromBody] TwoFactorVerifyRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (!user.TwoFactorEnabled)
            return BadRequest(new { msg = "2FA is not enabled" });

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return BadRequest(new { msg = "2FA secret not found" });

        if (!_twoFactorService.VerifyCode(user.TwoFactorSecret, request.Code))
            return BadRequest(new { msg = "Invalid 2FA code" });

        var backupCodes = _twoFactorService.GenerateBackupCodes();
        user.TwoFactorBackupCodes = JsonSerializer.Serialize(backupCodes);
        await _userManager.UpdateAsync(user);

        return Ok(new { backupCodes });
    }

    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        try
        {
            var response = await _authService.RefreshAsync();
            SetTokenCookie(response.Token);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { msg = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("signalr-token")]
    public async Task<IActionResult> GetSignalRToken()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        try
        {
            var response = await _authService.GenerateSignalRTokenAsync(userId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { msg = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var uid))
        {
            // Revoke all refresh tokens for this user
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == uid && !rt.IsRevoked)
                .ToListAsync();
            tokens.ForEach(t => t.IsRevoked = true);
            await _context.SaveChangesAsync();
        }
        ClearTokenCookie();
        ClearRefreshTokenCookie();
        return Ok(new { msg = "Logged out successfully" });
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        await _authService.RevokeTokensAsync(userId);
        ClearTokenCookie();
        ClearRefreshTokenCookie();
        return Ok(new { Message = "Tokens revoked" });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { msg = string.Join("; ", result.Errors.Select(e => e.Description)) });

        return Ok(new { msg = "Password changed successfully" });
    }

    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Ok(new ForgotPasswordResponse { Success = true, Message = "If the email exists, a reset token has been generated." });

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var frontendBaseUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:3000";
        _ = _emailService.SendPasswordResetEmailAsync(user.Email!, user.FullName ?? user.UserName ?? "", token, frontendBaseUrl);

        return Ok(new ForgotPasswordResponse
        {
            Success = true,
            Message = "If the email exists, a reset token has been generated."
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithTokenRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return BadRequest(new { msg = "Invalid email or token." });

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { msg = string.Join("; ", result.Errors.Select(e => e.Description)) });

        return Ok(new { msg = "Password reset successfully" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var roleName = roles.FirstOrDefault() ?? "operator";
        var isAdmin = roles.Any(r =>
            r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("SystemOwner", StringComparison.OrdinalIgnoreCase));

        // Normalize role name for frontend: SystemOwner maps to "admin" (frontend checks role === 'admin')
        var frontendRole = isAdmin ? "admin" : (roleName ?? "operator").ToLowerInvariant();

        var permissions = await BuildPermissionsDictAsync(user, isAdmin);

        return Ok(new
        {
            id = user.Id,
            name = user.FullName,
            userName = user.UserName,
            email = user.Email,
            phoneNumber = user.PhoneNumber,
            isActive = user.IsActive,
            createdAt = user.CreatedAt,
            role = frontendRole,
            roles,
            permissions,
            mustChangePassword = false,
            twoFactorEnabled = user.TwoFactorEnabled,
        });
    }

    private async Task<Dictionary<string, Dictionary<string, bool>>> BuildPermissionsDictAsync(User user, bool isAdmin)
    {
        try
        {
            var modules = await _context.ModuleDefinitions
                .Where(m => m.IsActive)
                .Include(m => m.PagePermissions.Where(pp => pp.IsActive))
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            if (isAdmin)
            {
                return modules.ToDictionary(
                    m => m.Key,
                    m => m.PagePermissions.ToDictionary(
                        pp => pp.ActionKey,
                        _ => true));
            }

            var roleNames = await _userManager.GetRolesAsync(user);
            var roleIds = new List<int>();
            foreach (var rn in roleNames)
            {
                var r = await _roleManager.FindByNameAsync(rn);
                if (r != null) roleIds.Add(r.Id);
            }

            var grantedPerms = await _context.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId) && rp.IsGranted)
                .Include(rp => rp.PagePermission)
                    .ThenInclude(pp => pp.ModuleDefinition)
                .Select(rp => new { rp.PagePermission.ModuleDefinition.Key, rp.PagePermission.ActionKey })
                .Distinct()
                .ToListAsync();

            var grantedDict = grantedPerms
                .GroupBy(g => g.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(x => x.ActionKey, _ => true));

            return modules.ToDictionary(
                m => m.Key,
                m =>
                {
                    if (grantedDict.TryGetValue(m.Key, out var actions))
                        return actions;
                    return m.PagePermissions.ToDictionary(pp => pp.ActionKey, _ => false);
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Auth] BuildPermissionsDictAsync error: {ex.Message}");
            return new Dictionary<string, Dictionary<string, bool>>();
        }
    }

    private void SetTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(60),
            Path = "/"
        };
        Response.Cookies.Append("access_token", token, cookieOptions);
    }

    private void ClearTokenCookie()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(-1),
            Path = "/"
        };
        Response.Cookies.Append("access_token", "", cookieOptions);
    }

    private void ClearRefreshTokenCookie()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(-1),
            Path = "/api/auth"
        };
        Response.Cookies.Append("refresh_token", "", cookieOptions);
    }
}
