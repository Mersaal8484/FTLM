using Microsoft.EntityFrameworkCore;
using Npgsql;
using FuelTrack.Domain.MultiTenancy;
using FuelTrack.Platform;

// This executable runs only against the disposable PostgreSQL database in compose.yaml.
var adminConnection = Environment.GetEnvironmentVariable("PROOF_DATABASE")
    ?? throw new InvalidOperationException("PROOF_DATABASE is required.");
Require(TenantIdentity.NormalizeHost(" Client.Example.COM. ") == "client.example.com", "Host normalization failed.");
Require(TenantIdentity.SchemaName(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")) == "tenant_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Schema name must derive from tenant ID.");
foreach (var badHost in new[] { "client.example.com:443", "https://client.example.com", "*.example.com" })
{
    try { TenantIdentity.NormalizeHost(badHost); throw new InvalidOperationException("Invalid host accepted."); }
    catch (ArgumentException) { }
}
var lifecycle = new PlatformTenant(Guid.NewGuid(), " Example ", "Client.Example.com");
Require(lifecycle.Status == PlatformTenantStatus.Provisioning && lifecycle.Host == "client.example.com", "Tenant setup failed.");
try { lifecycle.Archive(); throw new InvalidOperationException("An unprovisioned tenant was archived."); }
catch (InvalidOperationException ex) when (ex.Message != "An unprovisioned tenant was archived.") { }
lifecycle.Activate();
lifecycle.Suspend();
lifecycle.Activate();
lifecycle.Suspend();
lifecycle.Archive();
Require(lifecycle.Status == PlatformTenantStatus.Archived, "Tenant lifecycle failed.");
var tenants = new[]
{
    new Tenant("tenant_1", "proof_tenant_1", "proof_tenant_1_password"),
    new Tenant("tenant_2", "proof_tenant_2", "proof_tenant_2_password")
};

await using var admin = new NpgsqlConnection(adminConnection);
await admin.OpenAsync();
await ExecuteAsync(admin, "REVOKE CREATE ON SCHEMA public FROM PUBLIC;");

var platformOptions = new DbContextOptionsBuilder<PlatformDbContext>()
    .UseNpgsql(adminConnection, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "platform"))
    .Options;
await using (var platform = new PlatformDbContext(platformOptions))
{
    await platform.Database.MigrateAsync();
    await platform.Database.MigrateAsync();
    var registered = new PlatformTenant(Guid.NewGuid(), "First customer", "FIRST.example.com");
    platform.Tenants.Add(registered);
    var plan = new SubscriptionPlan(Guid.NewGuid(), "Basic", 10m, "usd");
    platform.Plans.Add(plan);
    platform.Subscriptions.Add(new PlatformSubscription(Guid.NewGuid(), registered.Id, plan.Id,
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));
    await platform.SaveChangesAsync();
    platform.ChangeTracker.Clear();
    var stored = await platform.Tenants.SingleAsync(x => x.Host == "first.example.com");
    Require(stored.Id == registered.Id && stored.Status == PlatformTenantStatus.Provisioning,
        "Platform tenant persistence failed.");
    Require(await platform.Subscriptions.CountAsync(x => x.TenantId == registered.Id) == 1,
        "Subscription persistence failed.");
    var resolver = new TenantAccessResolver(platform);
    Require(await resolver.ResolveAsync("first.example.com", DateTimeOffset.UtcNow) is null,
        "A provisioning tenant was granted access.");
    stored.Activate();
    await platform.SaveChangesAsync();
    var access = await resolver.ResolveAsync("FIRST.EXAMPLE.COM.", DateTimeOffset.UtcNow);
    Require(access?.TenantId == stored.Id && access.Schema == TenantIdentity.SchemaName(stored.Id), "Active tenant resolution failed.");
    Require(await resolver.ResolveAuthenticatedAsync("first.example.com", stored.Id.ToString(), DateTimeOffset.UtcNow) is not null, "Matching authenticated tenant rejected.");
    Require(await resolver.ResolveAuthenticatedAsync("first.example.com", Guid.NewGuid().ToString(), DateTimeOffset.UtcNow) is null, "Cross-tenant claim accepted.");
    Require(await resolver.ResolveAuthenticatedAsync("first.example.com", null, DateTimeOffset.UtcNow) is null, "Missing tenant claim accepted.");
    Require(await resolver.ResolveAsync("other.example.com", DateTimeOffset.UtcNow) is null, "Unknown host was accepted.");
    Require(await resolver.ResolveAsync("https://first.example.com", DateTimeOffset.UtcNow) is null, "Invalid host was accepted.");
    Require(await resolver.ResolveAsync("first.example.com", DateTimeOffset.UtcNow.AddDays(31)) is null, "Expired subscription was accepted.");
    platform.Tenants.Add(new PlatformTenant(Guid.NewGuid(), "Duplicate host", "FIRST.EXAMPLE.COM"));
    try
    {
        await platform.SaveChangesAsync();
        throw new InvalidOperationException("Duplicate host was accepted.");
    }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        // Normalized host names must be unique in the platform database.
    }
}

foreach (var tenant in tenants)
{
    // Identifiers are fixed proof fixtures; production identifiers must be generated
    // by the platform, validated and quoted, never taken from request input.
    await ExecuteAsync(admin, $"CREATE ROLE {tenant.Role} LOGIN PASSWORD '{tenant.Password}';");
    await ExecuteAsync(admin, $"CREATE SCHEMA {tenant.Schema} AUTHORIZATION proof_admin;");
    await ExecuteAsync(admin, $"REVOKE ALL ON SCHEMA {tenant.Schema} FROM PUBLIC;");
    await MigrateAsync(admin, tenant);
    await MigrateAsync(admin, tenant); // Re-running a completed migration is safe.
}

// Different processes may try to upgrade the same tenant at the same time.
await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
{
    await using var connection = new NpgsqlConnection(adminConnection);
    await connection.OpenAsync();
    await MigrateAsync(connection, tenants[0]);
}));

// DDL and migration history must roll back together on a failed upgrade.
try
{
    await using var transaction = await admin.BeginTransactionAsync();
    await ExecuteAsync(admin, $"CREATE TABLE {tenants[0].Schema}.\"FailedUpgrade\" (\"Id\" integer);");
    await ExecuteAsync(admin, $"INSERT INTO {tenants[0].Schema}.\"SchemaVersions\" VALUES (2);");
    throw new InvalidOperationException("Simulated migration failure");
}
catch (InvalidOperationException ex) when (ex.Message == "Simulated migration failure") { }
var rolledBack = (bool)(await new NpgsqlCommand(
    "SELECT to_regclass('tenant_1.\"FailedUpgrade\"') IS NULL", admin).ExecuteScalarAsync())!;
Require(rolledBack, "Failed migration left behind database objects.");

await Task.WhenAll(tenants.Select(async (tenant, i) =>
{
    await using var db = CreateContext(adminConnection, tenant);
    db.Entries.Add(new Entry { Name = $"tenant-{i + 1}" });
    await db.SaveChangesAsync();
}));

// Verify the EF model is reused safely and parallel requests see only their tenant.
await Task.WhenAll(Enumerable.Range(0, 30).Select(async i =>
{
    var tenant = tenants[i % tenants.Length];
    await using var db = CreateContext(adminConnection, tenant);
    var values = await db.Entries.AsNoTracking().Select(x => x.Name).ToListAsync();
    Require(values.Count == 1 && values[0] == $"tenant-{(i % tenants.Length) + 1}",
        "Concurrent tenant data was not isolated.");
}));

foreach (var tenant in tenants)
{
    var other = tenants.Single(x => x != tenant);
    await using var connection = new NpgsqlConnection(ConnectionString(adminConnection, tenant));
    await connection.OpenAsync();
    try
    {
        await ExecuteAsync(connection, $"SELECT * FROM {other.Schema}.\"Entries\";");
        throw new InvalidOperationException("A tenant role could access another tenant's schema.");
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InsufficientPrivilege)
    {
        // Expected: PostgreSQL privileges, not merely search_path, deny access.
    }
    var count = (long)(await new NpgsqlCommand($"SELECT count(*) FROM {tenant.Schema}.\"SchemaVersions\"", admin).ExecuteScalarAsync())!;
    Require(count == 1, "Migration history is not isolated or idempotent.");
}

Console.WriteLine("PASS: schema privileges, parallel EF reads, pooling, concurrent and failed migrations, and per-tenant history.");

static async Task MigrateAsync(NpgsqlConnection connection, Tenant tenant)
{
    await using var transaction = await connection.BeginTransactionAsync();
    // The lock makes concurrent provisioning for the same tenant serialize.
    await ExecuteAsync(connection, $"SELECT pg_advisory_xact_lock(hashtext('{tenant.Schema}'));");
    await ExecuteAsync(connection, $"""
        CREATE TABLE IF NOT EXISTS {tenant.Schema}."SchemaVersions" ("Version" int PRIMARY KEY);
        CREATE TABLE IF NOT EXISTS {tenant.Schema}."Entries" ("Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, "Name" text NOT NULL);
        INSERT INTO {tenant.Schema}."SchemaVersions" ("Version") VALUES (1) ON CONFLICT DO NOTHING;
        GRANT USAGE ON SCHEMA {tenant.Schema} TO {tenant.Role};
        GRANT SELECT, INSERT, UPDATE, DELETE ON {tenant.Schema}."Entries" TO {tenant.Role};
        GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA {tenant.Schema} TO {tenant.Role};
        """);
    await transaction.CommitAsync();
}

static ProofContext CreateContext(string adminConnection, Tenant tenant)
{
    var options = new DbContextOptionsBuilder<ProofContext>()
        .UseNpgsql(ConnectionString(adminConnection, tenant)).Options;
    return new ProofContext(options);
}

static string ConnectionString(string adminConnection, Tenant tenant) => new NpgsqlConnectionStringBuilder(adminConnection)
{
    Username = tenant.Role,
    Password = tenant.Password,
    SearchPath = tenant.Schema,
    MaxPoolSize = 4
}.ConnectionString;

static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
{
    await using var command = new NpgsqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

record Tenant(string Schema, string Role, string Password);
sealed class Entry { public int Id { get; set; } public string Name { get; set; } = ""; }
sealed class ProofContext(DbContextOptions<ProofContext> options) : DbContext(options)
{
    public DbSet<Entry> Entries => Set<Entry>();
}
