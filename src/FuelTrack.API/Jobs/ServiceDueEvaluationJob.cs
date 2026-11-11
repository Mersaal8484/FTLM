using FuelTrack.Application.Interfaces;

namespace FuelTrack.API.Jobs;

/// <summary>
/// Hourly Hangfire background job evaluating generator service intervals and due alerts.
/// Active when multi-tenancy is disabled (when enabled, TenantMaintenanceScheduler dispatches per tenant).
/// </summary>
public sealed class ServiceDueEvaluationJob(
    IServiceDueEvaluationService evaluation,
    ILogger<ServiceDueEvaluationJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var count = await evaluation.EvaluateAllGeneratorsAsync(ct);
        logger.LogInformation("[JOB] Service-due evaluation completed with {AlertCount} notifications", count);
    }
}
