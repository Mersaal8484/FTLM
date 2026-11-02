using FuelTrack.Application.Interfaces;
using FuelTrack.Platform;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FuelTrack.API.MultiTenancy;

/// <summary>
/// Runs a single trusted tenant's archival work in its own dependency-injection scope.
/// The caller must obtain tenant IDs from the platform registry, never job parameters
/// supplied by an untrusted client. This runner is not scheduled while multi-tenancy is disabled.
/// </summary>
public sealed class TenantArchivalRunner(IServiceScopeFactory scopes, IConfiguration configuration)
{
    public async Task<bool> RunAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        await using var scope = scopes.CreateAsyncScope();
        var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var host = await platform.Tenants.AsNoTracking().Where(t => t.Id == tenantId).Select(t => t.Host).SingleOrDefaultAsync(cancellationToken);
        if (host is null) return false;
        var resolver = scope.ServiceProvider.GetRequiredService<TenantAccessResolver>();
        var access = await resolver.ResolveAsync(host, DateTimeOffset.UtcNow, cancellationToken);
        if (access?.TenantId != tenantId) return false;

        var adminConnection = configuration.GetConnectionString("Platform")
            ?? throw new InvalidOperationException("Platform connection is required for tenant jobs.");
        await using var lockConnection = new NpgsqlConnection(adminConnection);
        await lockConnection.OpenAsync(cancellationToken);
        var lockKey = $"fueltrack:archival:{tenantId:N}";
        await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(hashtextextended(@key, 0))", lockConnection);
        command.Parameters.AddWithValue("key", lockKey);
        if (!(bool)(await command.ExecuteScalarAsync(cancellationToken))!) return false;
        try
        {
            var password = await scope.ServiceProvider.GetRequiredService<TenantCredentialReader>().GetPasswordAsync(tenantId, cancellationToken);
            var template = configuration.GetConnectionString("TenantDatabase")
                ?? throw new InvalidOperationException("Tenant database connection template is required.");
            var connection = new NpgsqlConnectionStringBuilder(template) { Username = access.Schema, Password = password, SearchPath = access.Schema };
            scope.ServiceProvider.GetRequiredService<TenantRequestContext>().Initialize(tenantId, connection.ConnectionString);
            var archival = scope.ServiceProvider.GetRequiredService<IDataArchivalService>();
            await archival.ArchiveAuditLogsAsync(90, cancellationToken);
            await archival.ArchiveDailyReadingsAsync(365, cancellationToken);
            return true;
        }
        finally
        {
            // Unlock on the same connection even when the job is cancelled or fails.
            command.CommandText = "SELECT pg_advisory_unlock(hashtextextended(@key, 0))";
            await command.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }
}
