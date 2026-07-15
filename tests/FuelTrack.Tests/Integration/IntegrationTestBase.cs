using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FuelTrack.Application.Common;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Migrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FuelTrack.Tests.Integration;

public class IntegrationTestBase : IAsyncLifetime
{
    private static readonly string JwtKey = "IntegrationTest_SecretKey_32Chars_Long!!";
    protected const string TestPassword = "Test@123456";
    protected const string TestUserName = "testadmin";
    protected const string TestUserEmail = "testadmin@test.com";
    // Static DB name ensures same InMemory database across multiple host builds
    // (WebApplicationFactory may call ConfigureServices more than once)
    private readonly string _inMemoryDbName = $"IntegrationTestDb_{Guid.NewGuid()}";

    protected WebApplicationFactory<FuelTrack.API.Program> Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    protected IServiceProvider Services => Factory.Services;

    public async Task InitializeAsync()
    {
        Factory = new WebApplicationFactory<FuelTrack.API.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove ALL DbContext-related registrations (including internal EF Core configs)
                    var dbContextDescriptors = services
                        .Where(d =>
                            d.ServiceType == typeof(DbContextOptions<FuelTrackDbContext>) ||
                            d.ServiceType == typeof(DbContextOptionsBuilder<FuelTrackDbContext>) ||
                            d.ServiceType == typeof(FuelTrackDbContext) ||
                            (d.ImplementationType != null && d.ImplementationType == typeof(FuelTrackDbContext)))
                        .ToList();
                    foreach (var d in dbContextDescriptors) services.Remove(d);

                    // Also remove any IDbContextOptionsConfiguration registrations that retain SqlServer provider state
                    var optionsConfigDescriptors = services
                        .Where(d =>
                            d.ServiceType.FullName != null &&
                            d.ServiceType.FullName.Contains("FuelTrackDbContext"))
                        .ToList();
                    foreach (var d in optionsConfigDescriptors) services.Remove(d);

                    // Add InMemory DbContext
                    services.AddDbContext<FuelTrackDbContext>(options =>
                        options.UseInMemoryDatabase(_inMemoryDbName));

                    // Re-register Identity stores so UserStore resolves against InMemory context.
                    // AddIdentityCore (not AddIdentity) avoids duplicate "Identity.Application" scheme.
                    services.RemoveAll<IUserStore<User>>();
                    services.RemoveAll<IRoleStore<Role>>();
                    services.AddIdentityCore<User>(options =>
                    {
                        options.Password.RequireDigit = true;
                        options.Password.RequireLowercase = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequireNonAlphanumeric = false;
                        options.Password.RequiredLength = 6;
                        options.User.RequireUniqueEmail = true;
                    })
                    .AddRoles<Role>()
                    .AddEntityFrameworkStores<FuelTrackDbContext>()
                    .AddDefaultTokenProviders();

                    // Replace MigrationRunner — skip SQL Server migrations entirely
                    services.RemoveAll<MigrationRunner>();
                    services.AddSingleton<MigrationRunner, NoOpMigrationRunner>();

                    // Mock IRealtimeNotifier
                    services.RemoveAll<IRealtimeNotifier>();
                    services.AddSingleton<IRealtimeNotifier, NoOpRealtimeNotifier>();

                    // Mock IEmailService
                    services.RemoveAll<IEmailService>();
                    services.AddSingleton<IEmailService, NoOpEmailService>();

                    // Remove OpenTelemetry.Extensions.Hosting auto-registrations.
                    // The hosting package registers OpenTelemetryMetricsListener as
                    // IMetricsListener — which depends on MeterProvider. Remove all
                    // IMetricsListener registrations plus any OpenTelemetry-typed services.
                    var otelDescriptors = services
                        .Where(d =>
                            d.ServiceType.FullName != null &&
                            (d.ServiceType.FullName.Contains("OpenTelemetry") ||
                             d.ServiceType.FullName == "Microsoft.Extensions.Diagnostics.Metrics.IMetricsListener" ||
                             (d.ImplementationType != null && d.ImplementationType.FullName != null && d.ImplementationType.FullName.Contains("OpenTelemetry"))))
                        .ToList();
                    foreach (var d in otelDescriptors) services.Remove(d);

                    // Add memory cache for HybridCacheService
                    services.AddMemoryCache();
                    services.AddDistributedMemoryCache();

                    // Override JWT Bearer options AFTER Program.cs configures them.
                    // Program.cs reads JwtSettings:Key at startup and captures it in a local variable.
                    // ConfigureAppConfiguration may replace config, but the JWT handler already captured the old key.
                    // PostConfigure runs after all initial configuration, ensuring our test key is used.
                    services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.TokenValidationParameters.IssuerSigningKey =
                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
                        options.TokenValidationParameters.ValidIssuer = "FuelTrack.API";
                        options.TokenValidationParameters.ValidAudience = "FuelTrack.Clients";
                    });
                });

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.Sources.Clear();
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Server=unused",
                        ["JwtSettings:Key"] = JwtKey,
                        ["JwtSettings:Issuer"] = "FuelTrack.API",
                        ["JwtSettings:Audience"] = "FuelTrack.Clients",
                        ["JwtSettings:AccessTokenExpirationMinutes"] = "60",
                        ["JwtSettings:RefreshTokenExpirationDays"] = "7",
                        ["AdminSettings:DefaultPassword"] = TestPassword,
                        ["Cors:Origins:0"] = "http://localhost:3000",
                        ["FrontendBaseUrl"] = "http://localhost:3000",
                        ["Smtp:Host"] = "localhost",
                        ["Smtp:Port"] = "587",
                        ["Smtp:From"] = "test@test.com",
                        ["Smtp:FromName"] = "Test",
                        ["DataEncryption:Key"] = "dGVzdGtleWZvcmVuY3J5cHRpb24xMjM0NTY=",
                    });
                });

                builder.UseEnvironment("Testing");
            });

        // Seed test user and database directly via context to avoid UserManager DI issues
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();

        await context.Database.EnsureCreatedAsync();

        // Create Admin role directly in context
        var existingRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (existingRole == null)
        {
            existingRole = new Role
            {
                Name = "Admin",
                NormalizedName = "ADMIN",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Roles.Add(existingRole);
            await context.SaveChangesAsync();
        }

        // Create test user directly in context with pre-hashed password
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == TestUserName);
        if (existingUser == null)
        {
            var hasher = new PasswordHasher<User>();
            existingUser = new User
            {
                UserName = TestUserName,
                Email = TestUserEmail,
                NormalizedUserName = TestUserName.ToUpper(),
                NormalizedEmail = TestUserEmail.ToUpper(),
                FullName = "Test Admin",
                SecurityStamp = Guid.NewGuid().ToString(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 0
            };
            existingUser.PasswordHash = hasher.HashPassword(existingUser, TestPassword);
            context.Users.Add(existingUser);
            await context.SaveChangesAsync();
        }

        // Link user to Admin role
        var hasRole = await context.UserRoles.AnyAsync(ur => ur.UserId == existingUser.Id && ur.RoleId == existingRole.Id);
        if (!hasRole)
        {
            context.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<int> { UserId = existingUser.Id, RoleId = existingRole.Id });
            await context.SaveChangesAsync();
        }

        // Also create and link SystemOwner role — PermissionEnforcementMiddleware bypasses for SystemOwner
        var systemOwnerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "SystemOwner");
        if (systemOwnerRole == null)
        {
            systemOwnerRole = new Role
            {
                Name = "SystemOwner",
                NormalizedName = "SYSTEMOWNER",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Roles.Add(systemOwnerRole);
            await context.SaveChangesAsync();
        }
        var hasSystemOwnerRole = await context.UserRoles.AnyAsync(ur => ur.UserId == existingUser.Id && ur.RoleId == systemOwnerRole.Id);
        if (!hasSystemOwnerRole)
        {
            context.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<int> { UserId = existingUser.Id, RoleId = systemOwnerRole.Id });
            await context.SaveChangesAsync();
        }

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();
        await Task.CompletedTask;
    }

    protected string GenerateJwtToken(int userId, string userName, string role, bool includeSystemOwner = true)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (includeSystemOwner)
            claims.Add(new Claim(ClaimTypes.Role, "SystemOwner")); // Bypass PermissionEnforcementMiddleware

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(60);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "FuelTrack.API",
            audience: "FuelTrack.Clients",
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    protected HttpClient CreateClientWithToken(string token)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        // Bypass FindByNameAsync — use a fresh scope to get the seeded user ID directly.
        // In .NET 10 minimal hosting, WebApplicationFactory.ConfigureServices runs before
        // Program.cs, so AddInfrastructureServices re-registers SQL Server DbContext +
        // Identity/UserStore after our InMemory override. The UserManager in request scope
        // resolves the SQL Server chain. Work around by reading user ID from DB directly.
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == TestUserName);
        if (user == null) throw new InvalidOperationException("Test user not found");

        var token = GenerateJwtToken(user.Id, user.UserName!, "Admin");
        return CreateClientWithToken(token);
    }

    // ---- No-op test doubles ----

    public class NoOpMigrationRunner : MigrationRunner
    {
        public NoOpMigrationRunner()
            : base(null!, null!, null!) { }

        public new Task RunAsync(int lockTimeoutMs = 60000, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private class NoOpRealtimeNotifier : IRealtimeNotifier
    {
        public Task NotifyAllAsync(string method, object? data) => Task.CompletedTask;
        public Task NotifyGroupAsync(string groupName, string method, object? data) => Task.CompletedTask;
        public Task NotifyUserAsync(string userId, string method, object? data) => Task.CompletedTask;
    }

    private class NoOpEmailService : IEmailService
    {
        public Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken, string frontendBaseUrl)
            => Task.FromResult(true);
        public Task<bool> SendWeeklyReportAsync(string toEmail, string userName, WeeklyReportEmailData data)
            => Task.FromResult(true);
        public Task<bool> SendFuelStockAlertAsync(string toEmail, string userName, FuelStockAlertEmailData data)
            => Task.FromResult(true);
    }
}
