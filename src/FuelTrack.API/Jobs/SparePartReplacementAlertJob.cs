using FuelTrack.Application.Interfaces;

namespace FuelTrack.API.Jobs;

public sealed class SparePartReplacementAlertJob
{
    private readonly IAlertGeneratorService _alerts;
    private readonly ILogger<SparePartReplacementAlertJob> _logger;

    public SparePartReplacementAlertJob(IAlertGeneratorService alerts, ILogger<SparePartReplacementAlertJob> logger)
    {
        _alerts = alerts;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var created = await _alerts.CheckInstalledPartsReplacementAsync(ct);
        _logger.LogInformation("[JOB] SparePartReplacementAlertJob created {AlertCount} in-app alerts", created);
    }
}
