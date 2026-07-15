using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.Jobs;

public class DatabaseBackupJob
{
    private readonly FuelTrackDbContext _db;
    private readonly ILogger<DatabaseBackupJob> _logger;

    public DatabaseBackupJob(FuelTrackDbContext db, ILogger<DatabaseBackupJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[JOB] Starting DatabaseBackupJob");

        try
        {
            await _db.Database.ExecuteSqlRawAsync("CHECKPOINT", ct);
            _logger.LogInformation("[JOB] DatabaseBackupJob completed — CHECKPOINT executed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JOB] DatabaseBackupJob failed");
            throw;
        }
    }
}
