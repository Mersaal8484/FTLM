using FuelTrack.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FuelTrack.API.Jobs;

public class DataArchivalJob
{
    private readonly IDataArchivalService _archivalService;
    private readonly ILogger<DataArchivalJob> _logger;

    public DataArchivalJob(IDataArchivalService archivalService, ILogger<DataArchivalJob> logger)
    {
        _archivalService = archivalService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[JOB] Starting DataArchivalJob");

        try
        {
            var auditLogsArchived = await _archivalService.ArchiveAuditLogsAsync(90, ct);
            var readingsArchived = await _archivalService.ArchiveDailyReadingsAsync(365, ct);

            _logger.LogInformation("[JOB] DataArchivalJob finished — Archived {AuditCount} audit logs, {ReadingsCount} daily readings",
                auditLogsArchived, readingsArchived);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JOB] DataArchivalJob encountered an error");
            throw;
        }
    }
}
