using FuelTrack.Application.Interfaces;

namespace FuelTrack.API.Jobs;

/// <summary>Uses the established Hangfire pipeline; it is not an ingest-time worker.</summary>
public sealed class OperatingLimitEvaluationJob(
    IFuelLoadSafetyService safety,
    ILogger<OperatingLimitEvaluationJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        await safety.EvaluateOperatingLimitsAsync(ct);
        logger.LogInformation("[JOB] Operating-limit evaluation completed");
    }
}
