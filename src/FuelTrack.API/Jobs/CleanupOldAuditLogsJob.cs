using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.Jobs;

public class CleanupOldAuditLogsJob
{
    private readonly FuelTrackDbContext _db;
    private readonly ILogger<CleanupOldAuditLogsJob> _logger;

    public CleanupOldAuditLogsJob(FuelTrackDbContext db, ILogger<CleanupOldAuditLogsJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[JOB] Starting CleanupOldAuditLogs");

        var cutoff = DateTime.UtcNow.AddDays(-90);
        var deleted = await _db.AuditLogs
            .Where(a => a.Timestamp < cutoff)
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation("[JOB] Removed {Count} audit logs older than {Cutoff}", deleted, cutoff);
    }
}
