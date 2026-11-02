using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.MultiTenancy;
using FuelTrack.Domain.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using FuelTrack.Platform;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using FuelTrack.Admin.Services;
using FuelTrack.API.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
using FuelTrack.API.Controllers;
using FuelTrack.API.Models;
using Microsoft.AspNetCore.Mvc;
using FuelTrack.Infrastructure.Services;
using FuelTrack.Application.Common;
using FuelTrack.Application.Interfaces;

if (args.Contains("--print-baseline"))
{
    await using var baseline = NewContext("Host=localhost;Database=placeholder;Username=placeholder;Password=placeholder");
    Console.Write(baseline.Database.GenerateCreateScript());
    return;
}
var adminString = Environment.GetEnvironmentVariable("PROOF_DATABASE") ?? throw new InvalidOperationException("PROOF_DATABASE is required.");
await using (var model = NewContext(adminString))
await using (var baseline = typeof(TenantDatabaseBootstrapper).Assembly.GetManifestResourceStream("FuelTrack.Tenants.001_initial_postgresql.sql") ?? throw new InvalidOperationException("Baseline missing."))
using (var reader = new StreamReader(baseline))
{
    var expected = (await reader.ReadToEndAsync()).Replace("\r\n", "\n").Trim();
    var actual = model.Database.GenerateCreateScript().Replace("\r\n", "\n").Trim();
    if (actual != expected) throw new InvalidOperationException("EF model differs from the initial PostgreSQL baseline; add a versioned upgrade and update the proof.");
}
await using (var sqlServer = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
    .UseSqlServer("Server=localhost;Database=unused;User Id=unused;Password=unused;TrustServerCertificate=True").Options))
{
    var script = sqlServer.Database.GenerateCreateScript();
    if (!script.Contains("[DeletedAt] IS NULL AND [IsApproved] = 1", StringComparison.Ordinal))
        throw new InvalidOperationException("SQL Server index filter changed unexpectedly.");
}
await using var admin = new NpgsqlConnection(adminString);
await admin.OpenAsync();
await Execute(admin, "REVOKE CREATE ON SCHEMA public FROM PUBLIC;");
var platformOptions = new DbContextOptionsBuilder<PlatformDbContext>()
    .UseNpgsql(adminString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "platform")).Options;
await using var platform = new PlatformDbContext(platformOptions);
await platform.Database.MigrateAsync();

foreach (var number in new[] { 1, 2 })
{
    await new TenantDatabaseBootstrapper().CreateAsync(adminString, TenantId(number), Password(number));
    await using var tenantDb = NewContext(TenantConnection(adminString, number));
    tenantDb.Users.Add(new User { UserName = $"tenant-{number}", NormalizedUserName = $"TENANT-{number}", Email = $"tenant-{number}@example.test", IsActive = true, FullName = $"Tenant {number}" });
    var branch = new Branch { Name = $"branch-{number}", Code = $"B{number}" };
    tenantDb.Branches.Add(branch);
    var station = new Station { Name = $"station-{number}", Code = $"S{number}", Branch = branch };
    tenantDb.Stations.Add(station);
    tenantDb.SubStations.Add(new SubStation { Name = $"substation-{number}", Code = $"SS{number}", Station = station });
    await tenantDb.SaveChangesAsync();
}

foreach (var number in new[] { 1, 2 })
{
    await using var db = NewContext(TenantConnection(adminString, number));
    db.AuditLogs.Add(new AuditLog { UserId = 1, Action = "proof", EntityName = "proof", Timestamp = DateTime.UtcNow.AddDays(-100), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
    await db.SaveChangesAsync();
}
await using (var db = NewContext(TenantConnection(adminString, 1)))
{
    var archived = await new DataArchivalService(db, NullLogger<DataArchivalService>.Instance).ArchiveAuditLogsAsync();
    if (archived != 1 || await db.AuditLogArchives.CountAsync() != 1) throw new InvalidOperationException("Tenant audit archival failed.");
}
await using (var db = NewContext(TenantConnection(adminString, 2)))
{
    if (await db.AuditLogs.CountAsync() != 1 || await db.AuditLogArchives.CountAsync() != 0)
        throw new InvalidOperationException("A tenant archival job changed another tenant's data.");
}

try
{
    await new TenantDatabaseBootstrapper().CreateAsync(adminString, TenantId(1), Password(1));
    throw new InvalidOperationException("Duplicate provisioning unexpectedly succeeded.");
}
catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateObject) { }

var migrations = new[] { new TenantSchemaUpgrade(2, "CREATE TABLE \"MigrationProbe\" (\"Id\" integer PRIMARY KEY);") };
await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => new TenantSchemaMigrator().UpgradeAsync(adminString, TenantId(1), migrations)));
try
{
    await new TenantSchemaMigrator().UpgradeAsync(adminString, TenantId(1), [migrations[0], new TenantSchemaUpgrade(3, "CREATE TABLE \"FailedProbe\" (\"Id\" integer); SELECT 1/0;")]);
    throw new InvalidOperationException("Failed migration unexpectedly succeeded.");
}
catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DivisionByZero) { }
await using (var check = new NpgsqlCommand($"SELECT count(*) FROM \"{Schema(1)}\".\"__TenantSchemaVersions\"", admin))
    if ((long)(await check.ExecuteScalarAsync())! != 2) throw new InvalidOperationException("Failed migration changed version history.");
await using (var check = new NpgsqlCommand($"SELECT to_regclass('{Schema(1)}.\"FailedProbe\"') IS NULL", admin))
    if (!(bool)(await check.ExecuteScalarAsync())!) throw new InvalidOperationException("Failed migration left a table behind.");
try
{
    await new TenantSchemaMigrator().UpgradeAsync(adminString, TenantId(1), [new TenantSchemaUpgrade(2, "SELECT 1")]);
    throw new InvalidOperationException("Modified migration was accepted.");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("history does not match", StringComparison.Ordinal)) { }

await Task.WhenAll(Enumerable.Range(0, 20).Select(async index =>
{
    var number = index % 2 + 1;
    await using var db = NewContext(TenantConnection(adminString, number));
    var names = await db.Users.AsNoTracking().Select(x => x.UserName).ToListAsync();
    if (names.Count != 1 || names[0] != $"tenant-{number}") throw new InvalidOperationException("Tenant users leaked across schemas.");
    var branches = await db.Branches.AsNoTracking().Select(x => x.Name).ToListAsync();
    if (branches.Count != 1 || branches[0] != $"branch-{number}") throw new InvalidOperationException("Tenant branches leaked across schemas.");
}));

foreach (var number in new[] { 1, 2 })
{
    await using var db = NewContext(TenantConnection(adminString, number));
    var scopeFilter = new UserScopeFilterService(db);
    var reports = new ReportService(db, new EmptyCache(), new TestUser(false), scopeFilter);
    var empty = await reports.GetEfficiencyReportAsync(null, null, null, null, null, 1, 20, "UTC");
    if (empty.Items.Count != 0) throw new InvalidOperationException("A user without scopes saw an efficiency report.");
    var dashboard = await reports.GetDashboardStatsAsync(null, null, null, null, null, "UTC");
    if (dashboard.System.TotalBranches != 0 || dashboard.System.TotalStations != 0 || dashboard.System.TotalSubStations != 0)
        throw new InvalidOperationException("A user without scopes saw dashboard counts.");
    var adminReports = new ReportService(db, new EmptyCache(), new TestUser(true), scopeFilter);
    var visible = await adminReports.GetEfficiencyReportAsync(null, null, null, null, null, 1, 20, "UTC");
    if (visible.Items.Count != 1 || visible.Items[0].SubStationName != $"substation-{number}")
        throw new InvalidOperationException("Admin report returned incorrect tenant data.");
}

await using (var restricted = new NpgsqlConnection(TenantConnection(adminString, 1)))
{
    await restricted.OpenAsync();
    await Execute(restricted, $"SELECT * FROM \"{Schema(1)}\".\"MigrationProbe\"");
    try { await Execute(restricted, $"SELECT * FROM \"{Schema(2)}\".\"Users\""); throw new InvalidOperationException("Cross-tenant access succeeded."); }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InsufficientPrivilege) { }
    try { await Execute(restricted, $"SELECT * FROM \"{Schema(1)}\".\"__TenantSchemaVersions\""); throw new InvalidOperationException("Tenant accessed protected schema history."); }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InsufficientPrivilege) { }
    try { await Execute(restricted, "SELECT * FROM platform.\"TenantCredentials\""); throw new InvalidOperationException("Tenant accessed platform credentials."); }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InsufficientPrivilege) { }
}

await using (var db = NewContext(TenantConnection(adminString, 1)))
{
    var user = await db.Users.SingleAsync();
    var original = await db.SubStations.Include(s => s.Station).SingleAsync();
    var sibling = new SubStation { Name = "sibling", Code = "SIB", StationId = original.StationId };
    var otherBranch = new Branch { Name = "second-branch", Code = "B2" };
    var otherStation = new Station { Name = "second-station", Code = "S2", Branch = otherBranch };
    var otherSub = new SubStation { Name = "second-substation", Code = "SS2", Station = otherStation };
    db.SubStations.AddRange(sibling, otherSub);
    await db.SaveChangesAsync();
    db.UserScopes.Add(new UserScope { UserId = user.Id, BranchId = original.Station.BranchId, StationId = original.StationId, SubStationId = original.Id });
    await db.SaveChangesAsync();
    var scopes = new UserScopeFilterService(db);
    if (await scopes.HasFullAccessToStationAsync(user.Id, original.StationId))
        throw new InvalidOperationException("Substation-only scope granted access to a whole station.");
    if ((await scopes.GetUserIdsWithAccessToSubStationAsync(sibling.Id)).Contains(user.Id))
        throw new InvalidOperationException("Substation-only user received another substation's notifications.");
    db.UserScopes.Add(new UserScope { UserId = user.Id, BranchId = otherBranch.Id });
    await db.SaveChangesAsync();
    var ids = await scopes.GetAccessibleSubStationIdsAsync(user.Id);
    if (!ids.Contains(original.Id) || !ids.Contains(otherSub.Id) || ids.Contains(sibling.Id))
        throw new InvalidOperationException("Multiple scopes did not form the correct union.");
    if (!(await scopes.GetUserIdsWithAccessToSubStationAsync(otherSub.Id)).Contains(user.Id))
        throw new InvalidOperationException("Branch-level access was not included in notification recipients.");
}

// Keep only ciphertext in platform metadata. The key is supplied separately.
var key = RandomNumberGenerator.GetBytes(32);
platform.Tenants.Add(new PlatformTenant(TenantId(1), "Proof tenant", "proof.example.test"));
var envelope = TenantCredentialProtector.Encrypt(TenantId(1), Password(1), "test-key-1", key);
platform.TenantCredentials.Add(envelope);
await platform.SaveChangesAsync();
if (TenantCredentialProtector.Decrypt(envelope, key) != Password(1)) throw new InvalidOperationException("Credential encryption round trip failed.");
if (System.Text.Encoding.UTF8.GetString(envelope.Ciphertext).Contains(Password(1), StringComparison.Ordinal)) throw new InvalidOperationException("Credential is stored as plaintext.");
try
{
    var wrongKey = RandomNumberGenerator.GetBytes(32);
    TenantCredentialProtector.Decrypt(envelope, wrongKey);
    throw new InvalidOperationException("Credential decryption accepted an invalid key.");
}
catch (CryptographicException) { }
try
{
    var swapped = new TenantCredential(TenantId(2), envelope.KeyId, envelope.Nonce, envelope.Ciphertext, envelope.Tag);
    TenantCredentialProtector.Decrypt(swapped, key);
    throw new InvalidOperationException("Credential accepted a different tenant ID.");
}
catch (CryptographicException) { }

var missingRoleId = Guid.NewGuid();
platform.Tenants.Add(new PlatformTenant(missingRoleId, "Missing role", "missing.example.test"));
platform.TenantCredentials.Add(TenantCredentialProtector.Encrypt(missingRoleId, Password(1), "test-key-1", key));
await platform.SaveChangesAsync();
try
{
    await new TenantCredentialRotator(platform).RotateAsync(missingRoleId, new string('d', 64), "test-key-1", key);
    throw new InvalidOperationException("Rotating a missing role unexpectedly succeeded.");
}
catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedObject) { }
platform.ChangeTracker.Clear();
var unchanged = await platform.TenantCredentials.SingleAsync(x => x.TenantId == missingRoleId);
if (TenantCredentialProtector.Decrypt(unchanged, key) != Password(1)) throw new InvalidOperationException("Failed rotation changed the stored credential.");

var newPassword = new string('c', 64);
await new TenantCredentialRotator(platform).RotateAsync(TenantId(1), newPassword, "test-key-1", key);
platform.ChangeTracker.Clear();
var firstTenantId = TenantId(1);
var rotated = await platform.TenantCredentials.SingleAsync(x => x.TenantId == firstTenantId);
if (TenantCredentialProtector.Decrypt(rotated, key) != newPassword) throw new InvalidOperationException("Credential envelope was not rotated.");
var oldConnection = new NpgsqlConnectionStringBuilder(TenantConnection(adminString, 1)) { Pooling = false };
try
{
    await using var old = new NpgsqlConnection(oldConnection.ConnectionString);
    await old.OpenAsync();
    throw new InvalidOperationException("Old tenant password still authenticates new connections.");
}
catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidPassword) { }
oldConnection.Password = newPassword;
await using (var renewed = new NpgsqlConnection(oldConnection.ConnectionString)) await renewed.OpenAsync();
var key2 = RandomNumberGenerator.GetBytes(32);
var secretsDirectory = Path.Combine(Path.GetTempPath(), "fueltrack-proof-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(secretsDirectory);
try
{
    var keyFile = Path.Combine(secretsDirectory, "key-1.key");
    await File.WriteAllTextAsync(keyFile, Convert.ToBase64String(key));
    File.SetUnixFileMode(keyFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    var fileProvider = new FileTenantCredentialKeyProvider(secretsDirectory);
    var loaded = await fileProvider.GetKeyAsync("key-1");
    if (loaded is null || !loaded.SequenceEqual(key)) throw new InvalidOperationException("Mounted secret key did not load.");
    if (await fileProvider.GetKeyAsync("missing") is not null) throw new InvalidOperationException("Missing key unexpectedly resolved.");
    try { await fileProvider.GetKeyAsync("../key-1"); throw new InvalidOperationException("Unsafe key identifier accepted."); }
    catch (ArgumentException) { }
    File.SetUnixFileMode(keyFile, UnixFileMode.UserRead | UnixFileMode.OtherRead);
    try { await fileProvider.GetKeyAsync("key-1"); throw new InvalidOperationException("Insecure secret permissions accepted."); }
    catch (InvalidOperationException ex) when (ex.Message.Contains("group or other", StringComparison.Ordinal)) { }
}
finally { Directory.Delete(secretsDirectory, recursive: true); }
var keyProvider = new TestKeys(new Dictionary<string, byte[]> { ["test-key-1"] = key, ["test-key-2"] = key2 });
var provisionTenantId = Guid.NewGuid();
platform.Tenants.Add(new PlatformTenant(provisionTenantId, "Provisioning proof", "provision.example.test"));
await platform.SaveChangesAsync();
var provisioner = new TenantProvisioningService(platform, keyProvider, "test-key-1");
if (!await provisioner.ProvisionAsync(provisionTenantId, "tenant-admin@example.test", "Tenant-Admin-Password-123!")) throw new InvalidOperationException("Initial provisioning was skipped.");
if (await provisioner.ProvisionAsync(provisionTenantId, "tenant-admin@example.test", "Tenant-Admin-Password-123!")) throw new InvalidOperationException("Repeated provisioning was not idempotent.");
platform.ChangeTracker.Clear();
var provisioned = await platform.Tenants.SingleAsync(t => t.Id == provisionTenantId);
if (provisioned.Status != PlatformTenantStatus.Provisioning) throw new InvalidOperationException("Provisioning enabled tenant access early.");
var provisionedPassword = await new TenantCredentialReader(platform, keyProvider).GetPasswordAsync(provisionTenantId);
var provisionedConn = new NpgsqlConnectionStringBuilder(adminString)
{
    Username = provisioned.Schema, SearchPath = provisioned.Schema, Password = provisionedPassword, Pooling = false
};
await using (var customerConnection = new NpgsqlConnection(provisionedConn.ConnectionString))
{
    await customerConnection.OpenAsync();
    await Execute(customerConnection, "SELECT count(*) FROM \"Users\"");
}
await using (var provisionedDb = NewContext(provisionedConn.ConnectionString))
{
    var administrator = await provisionedDb.Users.SingleAsync(u => u.UserName == "tenant-admin@example.test");
    if (new PasswordHasher<User>().VerifyHashedPassword(administrator, administrator.PasswordHash!, "Tenant-Admin-Password-123!") == PasswordVerificationResult.Failed)
        throw new InvalidOperationException("Tenant administrator password was not configured correctly.");
    var adminRole = await provisionedDb.Roles.SingleAsync(r => r.Name == "Admin");
    if (!await provisionedDb.Set<IdentityUserRole<int>>().AnyAsync(r => r.UserId == administrator.Id && r.RoleId == adminRole.Id) ||
        !await provisionedDb.UserScopes.AnyAsync(s => s.UserId == administrator.Id && s.BranchId == null && s.StationId == null && s.SubStationId == null) ||
        await provisionedDb.PagePermissions.CountAsync() == 0)
        throw new InvalidOperationException("Tenant administrator role, scope or permissions were not provisioned.");
}

// Only this disposable test activates the tenant to exercise request binding.
provisioned.Activate();
var testPlan = new SubscriptionPlan(Guid.NewGuid(), "Proof", 0m, "USD");
platform.Plans.Add(testPlan);
platform.Subscriptions.Add(new PlatformSubscription(Guid.NewGuid(), provisionTenantId, testPlan.Id, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));
await platform.SaveChangesAsync();
var middlewareConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:TenantDatabase"] = adminString
}).Build();
var nextCalled = false;
var requestState = new TenantRequestContext();
var request = new DefaultHttpContext();
request.Request.Host = new HostString("provision.example.test");
request.Request.Path = "/api/branches";
request.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", provisionTenantId.ToString("D"))], "test"));
var middleware = new TenantResolutionMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
await middleware.InvokeAsync(request, new TenantAccessResolver(platform), new TenantCredentialReader(platform, keyProvider), requestState, middlewareConfig);
if (!nextCalled || requestState.TenantId != provisionTenantId) throw new InvalidOperationException("Valid tenant request was rejected.");
var selected = new NpgsqlConnectionStringBuilder(requestState.DatabaseConnection);
if (selected.Username != provisioned.Schema || selected.SearchPath != provisioned.Schema || selected.Username == "proof_admin")
    throw new InvalidOperationException("Tenant request used an unrestricted database connection.");
await using (var selectedConnection = new NpgsqlConnection(selected.ConnectionString)) await selectedConnection.OpenAsync();
var tokenSettings = new Dictionary<string, string?>
{
    ["MultiTenancy:Enabled"] = "true",
    ["JwtSettings:Key"] = "proof-only-signing-key-with-at-least-32-characters!",
    ["JwtSettings:Issuer"] = "proof-issuer",
    ["JwtSettings:Audience"] = "proof-client",
    ["JwtSettings:SignalRAudience"] = "proof-signalr"
};
var tokenConfig = new ConfigurationBuilder().AddInMemoryCollection(tokenSettings).Build();
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IConfiguration>(tokenConfig);
services.AddSingleton<ITenantRequestContext>(requestState);
services.AddHttpContextAccessor();
services.AddDbContext<FuelTrackDbContext>(options => options.UseNpgsql(selected.ConnectionString));
services.AddIdentityCore<User>().AddRoles<Role>().AddEntityFrameworkStores<FuelTrackDbContext>();
services.AddScoped<ITwoFactorService, TwoFactorService>();
services.AddScoped<AuthService>();
ClaimsPrincipal? verifiedToken = null;
using (var provider = services.BuildServiceProvider())
using (var authScope = provider.CreateScope())
{
    var manager = authScope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var administrator = await manager.FindByNameAsync("tenant-admin@example.test") ?? throw new InvalidOperationException("Provisioned tenant administrator not found.");
    var auth = authScope.ServiceProvider.GetRequiredService<AuthService>();
    var token = await auth.LoginAsync("tenant-admin@example.test", "Tenant-Admin-Password-123!", null);
    var tokenHandler = new JwtSecurityTokenHandler();
    var verified = tokenHandler.ValidateToken(token.Token, new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = "proof-issuer", ValidateAudience = true, ValidAudience = "proof-client",
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSettings["JwtSettings:Key"]!)),
        ValidateLifetime = true, ClockSkew = TimeSpan.Zero
    }, out _);
    verifiedToken = verified;
    if (verified.FindFirst("tenant_id")?.Value != provisionTenantId.ToString("D"))
        throw new InvalidOperationException("Authenticated token is not bound to the tenant.");
    var verifiedRequest = new DefaultHttpContext();
    verifiedRequest.Request.Host = new HostString("provision.example.test");
    verifiedRequest.Request.Path = "/api/branches";
    verifiedRequest.User = verified;
    var verifiedTenant = new TenantRequestContext();
    nextCalled = false;
    await middleware.InvokeAsync(verifiedRequest, new TenantAccessResolver(platform), new TenantCredentialReader(platform, keyProvider), verifiedTenant, middlewareConfig);
    if (!nextCalled || verifiedTenant.TenantId != provisionTenantId)
        throw new InvalidOperationException("Verified login token did not resolve its tenant.");
    var signalR = await auth.GenerateSignalRTokenAsync(administrator.Id);
    var signalToken = tokenHandler.ReadJwtToken(signalR.Token);
    if (signalToken.Claims.Single(c => c.Type == "tenant_id").Value != provisionTenantId.ToString("D"))
        throw new InvalidOperationException("SignalR token is not bound to the tenant.");

    var totpSecret = new TwoFactorService().GenerateSecretKey();
    administrator.TwoFactorEnabled = true;
    administrator.TwoFactorSecret = totpSecret;
    if (!(await manager.UpdateAsync(administrator)).Succeeded) throw new InvalidOperationException("Could not configure 2FA test user.");
    try { await auth.BeginTwoFactorAsync("tenant-admin@example.test", "wrong-password"); throw new InvalidOperationException("2FA challenge was issued for an invalid password."); }
    catch (UnauthorizedAccessException) { }
    try { await auth.LoginAsync("tenant-admin@example.test", "Tenant-Admin-Password-123!", null); throw new InvalidOperationException("2FA was bypassed."); }
    catch (UnauthorizedAccessException) { }
    var failedChallenge = await auth.BeginTwoFactorAsync("tenant-admin@example.test", "Tenant-Admin-Password-123!");
    var accessor = authScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
    accessor.HttpContext = new DefaultHttpContext();
    accessor.HttpContext.Request.Headers.Cookie = $"refresh_token={failedChallenge}";
    try { await auth.RefreshAsync(); throw new InvalidOperationException("A 2FA challenge was accepted as a refresh token."); }
    catch (UnauthorizedAccessException) { }
    try { await auth.CompleteTwoFactorAsync(failedChallenge, "not-a-code", null); throw new InvalidOperationException("Invalid 2FA code was accepted."); }
    catch (UnauthorizedAccessException) { }
    var validCode = new Totp(Base32Encoding.ToBytes(totpSecret)).ComputeTotp();
    try { await auth.CompleteTwoFactorAsync(failedChallenge, validCode, null); throw new InvalidOperationException("Failed challenge was reused."); }
    catch (UnauthorizedAccessException) { }
    var staleChallenge = await auth.BeginTwoFactorAsync("tenant-admin@example.test", "Tenant-Admin-Password-123!");
    if (!(await manager.UpdateSecurityStampAsync(administrator)).Succeeded) throw new InvalidOperationException("Security stamp update failed.");
    try { await auth.CompleteTwoFactorAsync(staleChallenge, validCode, null); throw new InvalidOperationException("Challenge survived a security stamp change."); }
    catch (UnauthorizedAccessException) { }
    var successfulChallenge = await auth.BeginTwoFactorAsync("tenant-admin@example.test", "Tenant-Admin-Password-123!");
    var twoFactorToken = await auth.CompleteTwoFactorAsync(successfulChallenge, validCode, null);
    if (tokenHandler.ReadJwtToken(twoFactorToken.Token).Claims.Single(c => c.Type == "tenant_id").Value != provisionTenantId.ToString("D"))
        throw new InvalidOperationException("2FA token was not bound to its tenant.");
    try { await auth.CompleteTwoFactorAsync(successfulChallenge, validCode, null); throw new InvalidOperationException("Successful challenge was reused."); }
    catch (UnauthorizedAccessException) { }
    accessor.HttpContext = null;
    var concurrentChallenge = await auth.BeginTwoFactorAsync("tenant-admin@example.test", "Tenant-Admin-Password-123!");
    async Task<bool> CompleteOnce()
    {
        using var concurrentScope = provider.CreateScope();
        try { await concurrentScope.ServiceProvider.GetRequiredService<AuthService>().CompleteTwoFactorAsync(concurrentChallenge, validCode, null); return true; }
        catch (UnauthorizedAccessException) { return false; }
    }
    var concurrentResults = await Task.WhenAll(CompleteOnce(), CompleteOnce());
    if (concurrentResults.Count(result => result) != 1) throw new InvalidOperationException("Concurrent challenge reuse was not prevented.");
    var crossTenantChallenge = await auth.BeginTwoFactorAsync("tenant-admin@example.test", "Tenant-Admin-Password-123!");
    var otherServices = new ServiceCollection();
    otherServices.AddLogging();
    otherServices.AddSingleton<IConfiguration>(tokenConfig);
    otherServices.AddSingleton<ITenantRequestContext>(new TestTenant(firstTenantId, oldConnection.ConnectionString));
    otherServices.AddHttpContextAccessor();
    otherServices.AddDbContext<FuelTrackDbContext>(options => options.UseNpgsql(oldConnection.ConnectionString));
    otherServices.AddIdentityCore<User>().AddRoles<Role>().AddEntityFrameworkStores<FuelTrackDbContext>();
    otherServices.AddScoped<ITwoFactorService, TwoFactorService>();
    otherServices.AddScoped<AuthService>();
    using (var otherProvider = otherServices.BuildServiceProvider())
    using (var otherScope = otherProvider.CreateScope())
    {
        try { await otherScope.ServiceProvider.GetRequiredService<AuthService>().CompleteTwoFactorAsync(crossTenantChallenge, validCode, null); throw new InvalidOperationException("2FA challenge crossed tenant schemas."); }
        catch (UnauthorizedAccessException) { }
    }
    await auth.CompleteTwoFactorAsync(crossTenantChallenge, validCode, null);
    var controller = new AuthController(manager, authScope.ServiceProvider.GetRequiredService<RoleManager<Role>>(), auth,
        authScope.ServiceProvider.GetRequiredService<FuelTrackDbContext>(), null!, tokenConfig,
        authScope.ServiceProvider.GetRequiredService<ITwoFactorService>())
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };
    accessor.HttpContext = controller.HttpContext;
    if (await controller.Login(new LoginRequest { UserName = "tenant-admin@example.test", Password = "incorrect" }) is not UnauthorizedObjectResult)
        throw new InvalidOperationException("2FA login endpoint accepted a bad password.");
    var loginChallenge = await controller.Login(new LoginRequest { UserName = "tenant-admin@example.test", Password = "Tenant-Admin-Password-123!" });
    if (loginChallenge is not OkObjectResult { Value: TwoFactorLoginChallengeResponse challengeResponse } || string.IsNullOrWhiteSpace(challengeResponse.TempToken))
        throw new InvalidOperationException("2FA login endpoint did not issue a challenge.");
    var secondFactor = await controller.LoginWithTwoFactor(new TwoFactorVerifyRequest { TempToken = challengeResponse.TempToken, Code = validCode });
    if (secondFactor is not OkObjectResult) throw new InvalidOperationException("2FA endpoint rejected a valid code.");
    if (await controller.LoginWithTwoFactor(new TwoFactorVerifyRequest { TempToken = challengeResponse.TempToken, Code = validCode }) is not UnauthorizedObjectResult)
        throw new InvalidOperationException("2FA endpoint accepted a replayed challenge.");
}
var otherTenant = await platform.Tenants.SingleAsync(t => t.Id == firstTenantId);
otherTenant.Activate();
platform.Subscriptions.Add(new PlatformSubscription(Guid.NewGuid(), firstTenantId, testPlan.Id, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));
await platform.SaveChangesAsync();
var realCrossRequest = new DefaultHttpContext();
realCrossRequest.Request.Host = new HostString("proof.example.test");
realCrossRequest.Request.Path = "/api/branches";
realCrossRequest.User = verifiedToken ?? throw new InvalidOperationException("Verified login token missing.");
nextCalled = false;
await middleware.InvokeAsync(realCrossRequest, new TenantAccessResolver(platform), new TenantCredentialReader(platform, keyProvider), new TenantRequestContext(), middlewareConfig);
if (nextCalled || realCrossRequest.Response.StatusCode != StatusCodes.Status403Forbidden)
    throw new InvalidOperationException("A valid token was accepted on another tenant's registered host.");

await using (var tenantDb = NewContext(provisionedConn.ConnectionString))
{
    tenantDb.AuditLogs.Add(new AuditLog { UserId = 1, Action = "job-a", EntityName = "proof", Timestamp = DateTime.UtcNow.AddDays(-100), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
    await tenantDb.SaveChangesAsync();
}
await using (var tenantDb = NewContext(oldConnection.ConnectionString))
{
    tenantDb.AuditLogs.Add(new AuditLog { UserId = 1, Action = "job-b", EntityName = "proof", Timestamp = DateTime.UtcNow.AddDays(-100), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
    await tenantDb.SaveChangesAsync();
}
var jobSettings = new Dictionary<string, string?>
{
    ["MultiTenancy:Enabled"] = "true", ["ConnectionStrings:Platform"] = adminString, ["ConnectionStrings:TenantDatabase"] = adminString
};
var jobConfig = new ConfigurationBuilder().AddInMemoryCollection(jobSettings).Build();
var jobServices = new ServiceCollection();
jobServices.AddLogging();
jobServices.AddSingleton<IConfiguration>(jobConfig);
jobServices.AddSingleton<ITenantCredentialKeyProvider>(keyProvider);
jobServices.AddDbContext<PlatformDbContext>(o => o.UseNpgsql(adminString, p => p.MigrationsHistoryTable("__EFMigrationsHistory", "platform")));
jobServices.AddScoped<TenantCredentialReader>();
jobServices.AddScoped<TenantAccessResolver>();
jobServices.AddScoped<TenantRequestContext>();
jobServices.AddScoped<ITenantRequestContext>(sp => sp.GetRequiredService<TenantRequestContext>());
jobServices.AddInfrastructureServices(jobConfig);
using (var jobProvider = jobServices.BuildServiceProvider())
{
    var runner = new TenantArchivalRunner(jobProvider.GetRequiredService<IServiceScopeFactory>(), jobConfig);
    if (!await runner.RunAsync(provisionTenantId)) throw new InvalidOperationException("Tenant archival runner did not execute.");
}
await using (var db = NewContext(provisionedConn.ConnectionString))
    if (!await db.AuditLogArchives.AnyAsync(a => a.Action == "job-a")) throw new InvalidOperationException("Tenant job failed to archive its record.");
await using (var db = NewContext(oldConnection.ConnectionString))
    if (!await db.AuditLogs.AnyAsync(a => a.Action == "job-b") || await db.AuditLogArchives.AnyAsync(a => a.Action == "job-b"))
        throw new InvalidOperationException("Tenant job touched another tenant's records.");
nextCalled = false;
var crossRequest = new DefaultHttpContext();
crossRequest.Request.Host = new HostString("provision.example.test");
crossRequest.Request.Path = "/api/branches";
crossRequest.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", Guid.NewGuid().ToString("D"))], "test"));
await middleware.InvokeAsync(crossRequest, new TenantAccessResolver(platform), new TenantCredentialReader(platform, keyProvider), new TenantRequestContext(), middlewareConfig);
if (nextCalled || crossRequest.Response.StatusCode != StatusCodes.Status403Forbidden)
    throw new InvalidOperationException("Cross-tenant token was not rejected.");
foreach (var claims in new[] { Array.Empty<Claim>(), new[] { new Claim("tenant_id", provisionTenantId.ToString()), new Claim("tenant_id", provisionTenantId.ToString()) } })
{
    var invalid = new DefaultHttpContext();
    invalid.Request.Host = new HostString("provision.example.test");
    invalid.Request.Path = "/api/branches";
    invalid.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    await middleware.InvokeAsync(invalid, new TenantAccessResolver(platform), new TenantCredentialReader(platform, keyProvider), new TenantRequestContext(), middlewareConfig);
    if (invalid.Response.StatusCode != StatusCodes.Status403Forbidden) throw new InvalidOperationException("Missing or duplicated tenant claims were accepted.");
}
var unknown = new DefaultHttpContext();
unknown.Request.Host = new HostString("unknown.example.test");
await middleware.InvokeAsync(unknown, new TenantAccessResolver(platform), new TenantCredentialReader(platform, keyProvider), new TenantRequestContext(), middlewareConfig);
if (unknown.Response.StatusCode != StatusCodes.Status404NotFound) throw new InvalidOperationException("Unknown host was not rejected.");

var failedTenantId = Guid.NewGuid();
platform.Tenants.Add(new PlatformTenant(failedTenantId, "Failure proof", "failure.example.test"));
await platform.SaveChangesAsync();
try
{
    await new TenantProvisioningService(platform, keyProvider, "missing").ProvisionAsync(failedTenantId, "tenant-admin@example.test", "Tenant-Admin-Password-123!");
    throw new InvalidOperationException("Provisioning succeeded without an encryption key.");
}
catch (InvalidOperationException ex) when (ex.Message == "Tenant encryption key is unavailable.") { }
platform.ChangeTracker.Clear();
if (await platform.TenantCredentials.AnyAsync(x => x.TenantId == failedTenantId))
    throw new InvalidOperationException("Failed provisioning persisted a credential.");
await using (var failedSchema = new NpgsqlCommand("SELECT EXISTS (SELECT FROM pg_namespace WHERE nspname = @schema)", admin))
{
    failedSchema.Parameters.AddWithValue("schema", TenantIdentity.SchemaName(failedTenantId));
    if ((bool)(await failedSchema.ExecuteScalarAsync())!) throw new InvalidOperationException("Failed provisioning created a schema.");
}
var rollbackTenantId = Guid.NewGuid();
platform.Tenants.Add(new PlatformTenant(rollbackTenantId, "Rollback proof", "rollback.example.test"));
await platform.SaveChangesAsync();
await Execute(admin, $"""
    CREATE FUNCTION platform.proof_reject_credential() RETURNS trigger LANGUAGE plpgsql AS $$
    BEGIN IF NEW."TenantId" = '{rollbackTenantId:D}'::uuid THEN RAISE EXCEPTION 'proof failure'; END IF; RETURN NEW; END $$;
    CREATE TRIGGER proof_reject_credential BEFORE INSERT ON platform."TenantCredentials" FOR EACH ROW EXECUTE FUNCTION platform.proof_reject_credential();
    """);
try
{
    await provisioner.ProvisionAsync(rollbackTenantId, "tenant-admin@example.test", "Tenant-Admin-Password-123!");
    throw new InvalidOperationException("Simulated credential failure was accepted.");
}
catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.RaiseException) { }
platform.ChangeTracker.Clear();
await Execute(admin, "DROP TRIGGER proof_reject_credential ON platform.\"TenantCredentials\"; DROP FUNCTION platform.proof_reject_credential();");
if (await platform.TenantCredentials.AnyAsync(x => x.TenantId == rollbackTenantId)) throw new InvalidOperationException("Failed provisioning persisted a credential.");
await using (var rollbackCheck = new NpgsqlCommand("SELECT EXISTS (SELECT FROM pg_namespace WHERE nspname = @name) OR EXISTS (SELECT FROM pg_roles WHERE rolname = @name)", admin))
{
    rollbackCheck.Parameters.AddWithValue("name", TenantIdentity.SchemaName(rollbackTenantId));
    if ((bool)(await rollbackCheck.ExecuteScalarAsync())!) throw new InvalidOperationException("Failed provisioning did not roll back schema and role.");
}
if (await new TenantCredentialReader(platform, keyProvider).GetPasswordAsync(firstTenantId) != newPassword)
    throw new InvalidOperationException("Credential reader failed.");
await new TenantCredentialKeyRotator(platform, keyProvider).RewrapAsync(firstTenantId, "test-key-2");
platform.ChangeTracker.Clear();
var rewrapped = await platform.TenantCredentials.SingleAsync(x => x.TenantId == firstTenantId);
if (rewrapped.KeyId != "test-key-2" || TenantCredentialProtector.Decrypt(rewrapped, key2) != newPassword)
    throw new InvalidOperationException("Key rotation failed.");
try { TenantCredentialProtector.Decrypt(rewrapped, key); throw new InvalidOperationException("Old key still decrypts rotated envelope."); }
catch (CryptographicException) { }
try { await new TenantCredentialKeyRotator(platform, keyProvider).RewrapAsync(firstTenantId, "missing"); throw new InvalidOperationException("Missing key accepted."); }
catch (InvalidOperationException ex) when (ex.Message == "New encryption key is unavailable.") { }
platform.ChangeTracker.Clear();
if ((await platform.TenantCredentials.SingleAsync(x => x.TenantId == firstTenantId)).KeyId != "test-key-2")
    throw new InvalidOperationException("Failed key rotation changed the credential.");
Console.WriteLine("PASS: full FuelTrack PostgreSQL model, tenant isolation, encrypted credentials and atomic rotation.");

static FuelTrackDbContext NewContext(string connectionString) => new(new DbContextOptionsBuilder<FuelTrackDbContext>().UseNpgsql(connectionString).Options);
static Guid TenantId(int number) => Guid.Parse($"00000000-0000-0000-0000-{number:000000000000}");
static string Schema(int number) => TenantIdentity.SchemaName(TenantId(number));
static string Password(int number) => new(number == 1 ? 'a' : 'b', 64);
static string TenantConnection(string connectionString, int number) => new NpgsqlConnectionStringBuilder(connectionString)
{
    Username = Schema(number), Password = Password(number), SearchPath = Schema(number), MaxPoolSize = 4
}.ConnectionString;
static async Task Execute(NpgsqlConnection connection, string sql)
{
    await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 180 };
    await command.ExecuteNonQueryAsync();
}

sealed class TestKeys(IReadOnlyDictionary<string, byte[]> keys) : ITenantCredentialKeyProvider
{
    public ValueTask<byte[]?> GetKeyAsync(string keyId, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(keys.TryGetValue(keyId, out var key) ? key.ToArray() : null);
}
sealed class TestUser(bool admin) : ICurrentUserService
{
    public int? UserId => 1;
    public string? UserName => "proof";
    public bool IsAuthenticated => true;
    public bool IsAdmin => admin;
    public bool IsSystemOwner => false;
    public bool HasAnyAdminRole => admin;
}
sealed record TestTenant(Guid Id, string Connection) : ITenantRequestContext
{
    public Guid? TenantId => Id;
    public string? DatabaseConnection => Connection;
}
sealed class EmptyCache : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) => Task.FromResult<T?>(default);
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) => Task.CompletedTask;
    public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default) => Task.CompletedTask;
}
