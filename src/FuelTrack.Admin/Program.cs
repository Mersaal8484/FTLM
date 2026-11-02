using System.Security.Claims;
using System.Threading.RateLimiting;
using FuelTrack.Platform;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FuelTrack.Admin.Services;

if (args.Contains("--hash-password"))
{
    var password = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
        throw new InvalidOperationException("Enter a password of at least 12 characters on standard input.");
    Console.WriteLine(new PasswordHasher<object>().HashPassword(new object(), password));
    return;
}

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Platform")
    ?? throw new InvalidOperationException("ConnectionStrings:Platform is required.");
var username = builder.Configuration["PlatformAdmin:Username"]
    ?? throw new InvalidOperationException("PlatformAdmin:Username is required.");
var passwordHash = builder.Configuration["PlatformAdmin:PasswordHash"]
    ?? throw new InvalidOperationException("PlatformAdmin:PasswordHash is required.");
if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(passwordHash))
    throw new InvalidOperationException("Platform admin credentials cannot be empty.");

builder.Services.AddDbContext<PlatformDbContext>(options => options.UseNpgsql(connection,
    postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "platform")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "fueltrack.platform";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.LoginPath = "/login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = false;
});
builder.Services.AddAuthorization();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddSingleton(new AdminCredentials(username, passwordHash));
var keyDirectory = builder.Configuration["TenantProvisioning:KeyDirectory"];
var keyId = builder.Configuration["TenantProvisioning:KeyId"];
if (!string.IsNullOrWhiteSpace(keyDirectory) && !string.IsNullOrWhiteSpace(keyId))
{
    builder.Services.AddSingleton<ITenantCredentialKeyProvider>(new FileTenantCredentialKeyProvider(keyDirectory));
    builder.Services.AddScoped(sp => new TenantProvisioningService(sp.GetRequiredService<PlatformDbContext>(),
        sp.GetRequiredService<ITenantCredentialKeyProvider>(), keyId));
}

var app = builder.Build();
if (builder.Configuration.GetValue<bool>("Platform:AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<PlatformDbContext>().Database.MigrateAsync();
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();

public sealed record AdminCredentials(string Username, string PasswordHash);
