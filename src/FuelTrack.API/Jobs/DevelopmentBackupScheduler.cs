using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FuelTrack.API.Jobs;

// Development compose does not run Hangfire. Keep this scheduler limited to the
// single-instance development deployment so production continues to use Hangfire.
public sealed class DevelopmentBackupScheduler(IServiceScopeFactory scopeFactory, ILogger<DevelopmentBackupScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var next = now.Date.AddHours(2);
            if (next <= now.UtcDateTime) next = next.AddDays(1);
            logger.LogInformation("Next development database backup is scheduled for {NextRunUtc} UTC", next);
            try
            {
                await Task.Delay(next - now.UtcDateTime, stoppingToken);
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<DatabaseBackupJob>().ExecuteAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Scheduled development database backup failed"); }
        }
    }
}
