using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace FuelTrack.Admin.Pages;

[EnableRateLimiting("login")]
public sealed class LoginModel(AdminCredentials credentials) : PageModel
{
    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    public string? Error { get; private set; }
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        var verification = new PasswordHasher<object>().VerifyHashedPassword(new object(), credentials.PasswordHash, Password ?? "");
        if (!string.Equals(Username, credentials.Username, StringComparison.Ordinal) || verification == PasswordVerificationResult.Failed)
        {
            Error = "بيانات الدخول غير صحيحة";
            return Page();
        }
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, credentials.Username), new Claim(ClaimTypes.Role, "PlatformAdmin") }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return RedirectToPage("/Index");
    }
}
