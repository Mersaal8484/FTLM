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
        // Operating-limit evaluation needs an hourly cadence. The daily maintenance work
        // remains daily and is intentionally not run at process startup.
        DateOnly? lastDailyRun = null;
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOperatingLimitEvaluationsAsync(stoppingToken);
                await RunServiceDueEvaluationsAsync(stoppingToken);
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                if (lastDailyRun != today)
                {
                    await RunDailyMaintenanceAsync(stoppingToken);
                    lastDailyRun = today;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
            catch (Exception ex) { logger.LogError(ex, "Tenant maintenance dispatch failed"); }
        }
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        await RunDailyMaintenanceAsync(cancellationToken);
        await RunOperatingLimitEvaluationsAsync(cancellationToken);
        await RunServiceDueEvaluationsAsync(cancellationToken);
    }

    private async Task RunDailyMaintenanceAsync(CancellationToken cancellationToken)
    {
        var tenants = await GetEligibleTenantIdsAsync(cancellationToken);
        foreach (var tenantId in tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await runner.RunAsync(tenantId, cancellationToken);
                await runner.CleanupExpiredTokensAsync(tenantId, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) { logger.LogError(ex, "Tenant daily maintenance failed for {TenantId}", tenantId); }
        }
    }

    private async Task RunOperatingLimitEvaluationsAsync(CancellationToken cancellationToken)
    {
        var tenants = await GetEligibleTenantIdsAsync(cancellationToken);
        foreach (var tenantId in tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { await runner.EvaluateOperatingLimitsAsync(tenantId, cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) { logger.LogError(ex, "Operating-limit evaluation failed for tenant {TenantId}", tenantId); }
        }
    }

    private async Task RunServiceDueEvaluationsAsync(CancellationToken cancellationToken)
    {
        var tenants = await GetEligibleTenantIdsAsync(cancellationToken);
        foreach (var tenantId in tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { await runner.EvaluateServiceDueAsync(tenantId, cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) { logger.LogError(ex, "Service-due evaluation failed for tenant {TenantId}", tenantId); }
        }
    }

    private async Task<List<Guid>> GetEligibleTenantIdsAsync(CancellationToken cancellationToken)
    {
        await using (var scope = scopes.CreateAsyncScope())
        {
            var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var now = DateTimeOffset.UtcNow;
            return await platform.Tenants.AsNoTracking()
                .Where(t => t.Status == PlatformTenantStatus.Active &&
                    platform.Subscriptions.Any(s => s.TenantId == t.Id && s.StartsAt <= now && now < s.EndsAt))
                .Select(t => t.Id).ToListAsync(cancellationToken);
        }
    }
}
