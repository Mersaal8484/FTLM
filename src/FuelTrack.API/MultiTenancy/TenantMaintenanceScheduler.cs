using FuelTrack.Domain.MultiTenancy;
using FuelTrack.Platform;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.MultiTenancy;

/// <summary>Dispatches idempotent maintenance to subscribed tenants using isolated scopes.</summary>
public sealed class TenantMaintenanceScheduler(
    IServiceScopeFactory scopes,
    TenantArchivalRunner runner,
    ILogger<TenantMaintenanceScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Explicitly enabled deployments run daily; avoid an unexpected job at startup.
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
            catch (Exception ex) { logger.LogError(ex, "Tenant maintenance dispatch failed"); }
        }
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        List<Guid> tenants;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var now = DateTimeOffset.UtcNow;
            tenants = await platform.Tenants.AsNoTracking()
                .Where(t => t.Status == PlatformTenantStatus.Active &&
                    platform.Subscriptions.Any(s => s.TenantId == t.Id && s.StartsAt <= now && now < s.EndsAt))
                .Select(t => t.Id).ToListAsync(cancellationToken);
        }
        foreach (var tenantId in tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await runner.RunAsync(tenantId, cancellationToken);
                await runner.CleanupExpiredTokensAsync(tenantId, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) { logger.LogError(ex, "Tenant maintenance failed for {TenantId}", tenantId); }
        }
    }
}
