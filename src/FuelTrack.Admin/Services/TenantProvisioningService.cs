using System.Security.Cryptography;
using FuelTrack.Domain.MultiTenancy;
using FuelTrack.Infrastructure.MultiTenancy;
using FuelTrack.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Data;
using FuelTrack.Infrastructure.Services;
using FuelTrack.Application.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Mail;

namespace FuelTrack.Admin.Services;

/// <summary>Creates the schema, role and encrypted credential in one PostgreSQL transaction.</summary>
public sealed class TenantProvisioningService(PlatformDbContext db, ITenantCredentialKeyProvider keys, string keyId)
{
    public async Task<bool> ProvisionAsync(Guid tenantId, string adminEmail, string adminPassword, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var tenant = await db.Tenants.FromSqlInterpolated($"SELECT * FROM platform.\"Tenants\" WHERE \"Id\" = {tenantId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Tenant does not exist.");
        if (tenant.Status != PlatformTenantStatus.Provisioning)
            throw new InvalidOperationException("Tenant is not awaiting provisioning.");
        if (await db.TenantCredentials.AnyAsync(x => x.TenantId == tenantId, cancellationToken))
        {
            // Do not claim success based solely on a credential record: an operator
            // must investigate inconsistent or partially restored database state.
            var schema = TenantIdentity.SchemaName(tenantId);
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await using var check = connection.CreateCommand();
            check.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            check.CommandText = "SELECT EXISTS (SELECT FROM pg_namespace WHERE nspname = @schema) AND EXISTS (SELECT FROM pg_roles WHERE rolname = @schema)";
            check.Parameters.AddWithValue("schema", schema);
            if (!(bool)(await check.ExecuteScalarAsync(cancellationToken))!)
                throw new InvalidOperationException("Stored credential does not match the database state.");
            check.Parameters.Clear();
            check.CommandText = $"SELECT EXISTS (SELECT FROM \"{schema}\".\"__TenantSchemaVersions\" WHERE \"Version\" = 1)";
            if (!(bool)(await check.ExecuteScalarAsync(cancellationToken))!)
                throw new InvalidOperationException("Tenant schema version history is missing.");
            check.CommandText = $"""
                SELECT EXISTS (
                    SELECT FROM "{schema}"."Users" u
                    JOIN "{schema}"."UserRoles" ur ON ur."UserId" = u."Id"
                    JOIN "{schema}"."Roles" r ON r."Id" = ur."RoleId"
                    JOIN "{schema}"."UserScopes" s ON s."UserId" = u."Id"
                    WHERE u."IsActive" AND r."NormalizedName" = 'ADMIN'
                        AND s."DeletedAt" IS NULL AND s."BranchId" IS NULL AND s."StationId" IS NULL AND s."SubStationId" IS NULL)
                """;
            if (!(bool)(await check.ExecuteScalarAsync(cancellationToken))!)
                throw new InvalidOperationException("Tenant administrator is missing or incomplete.");
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        if (string.IsNullOrWhiteSpace(adminEmail) || adminEmail.Length > 256 || !MailAddress.TryCreate(adminEmail, out var parsedEmail) ||
            !string.Equals(parsedEmail.Address, adminEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A valid tenant administrator email is required.", nameof(adminEmail));
        if (string.IsNullOrWhiteSpace(adminPassword) || adminPassword.Length < 12 || adminPassword.Length > 128 ||
            !adminPassword.Any(char.IsUpper) || !adminPassword.Any(char.IsLower) || !adminPassword.Any(char.IsDigit) || !adminPassword.Any(c => !char.IsLetterOrDigit(c)))
            throw new ArgumentException("Tenant administrator password does not meet policy.", nameof(adminPassword));

        var key = await keys.GetKeyAsync(keyId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant encryption key is unavailable.");
        try
        {
            var random = RandomNumberGenerator.GetBytes(32);
            string password;
            try { password = Convert.ToHexString(random).ToLowerInvariant(); }
            finally { CryptographicOperations.ZeroMemory(random); }
            var credential = TenantCredentialProtector.Encrypt(tenantId, password, keyId, key);
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await new TenantDatabaseBootstrapper().CreateWithinTransactionAsync(connection,
                (NpgsqlTransaction)transaction.GetDbTransaction(), tenantId, password, cancellationToken);
            await SeedTenantAdminAsync(connection, (NpgsqlTransaction)transaction.GetDbTransaction(), adminEmail.Trim(), adminPassword, cancellationToken);
            db.TenantCredentials.Add(credential);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    private static async Task SeedTenantAdminAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string email, string password, CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<FuelTrackDbContext>(options => options.UseNpgsql(connection));
        services.AddIdentityCore<User>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
        }).AddRoles<Role>().AddEntityFrameworkStores<FuelTrackDbContext>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var tenantDb = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        await tenantDb.Database.UseTransactionAsync(transaction, cancellationToken);
        await ModuleRegistrySeed.SeedAsync(tenantDb);
        await new PermissionGenerator(tenantDb).GenerateAllPermissionsAsync();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        await new RoleService(roles, users, tenantDb, new NoCache()).InitAsync();
        var now = DateTime.UtcNow;
        var admin = new User { UserName = email, Email = email, FullName = "Tenant Administrator", IsActive = true, CreatedAt = now, UpdatedAt = now };
        var created = await users.CreateAsync(admin, password);
        if (!created.Succeeded) throw new InvalidOperationException("Could not create the tenant administrator.");
        var assigned = await users.AddToRoleAsync(admin, "Admin");
        if (!assigned.Succeeded) throw new InvalidOperationException("Could not assign the tenant administrator role.");
        tenantDb.UserScopes.Add(new UserScope { UserId = admin.Id, CreatedAt = now, UpdatedAt = now });
        await tenantDb.SaveChangesAsync(cancellationToken);
    }

    private sealed class NoCache : ICacheService
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default) => Task.CompletedTask;
    }
}
