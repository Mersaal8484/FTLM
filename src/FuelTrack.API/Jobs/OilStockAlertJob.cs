using FuelTrack.Application.Interfaces;

namespace FuelTrack.API.Jobs;

public sealed class OilStockAlertJob
{
    private readonly IAlertGeneratorService _alerts;
    private readonly ILogger<OilStockAlertJob> _logger;

    public OilStockAlertJob(IAlertGeneratorService alerts, ILogger<OilStockAlertJob> logger)
    {
        _alerts = alerts;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var created = await _alerts.CheckOilStockLevelsAsync(ct);
        _logger.LogInformation("[JOB] OilStockAlertJob created {AlertCount} in-app alerts", created);
    }
}
