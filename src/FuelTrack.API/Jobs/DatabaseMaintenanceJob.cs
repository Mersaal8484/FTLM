using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.Jobs;

public class DatabaseMaintenanceJob
{
    private readonly FuelTrackDbContext _db;
    private readonly ILogger<DatabaseMaintenanceJob> _logger;

    public DatabaseMaintenanceJob(FuelTrackDbContext db, ILogger<DatabaseMaintenanceJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[JOB] Starting DatabaseMaintenance");

        try
        {
            if (_db.Database.IsRelational())
            {
                await _db.Database.ExecuteSqlRawAsync("EXEC sp_updatestats", ct);
                _logger.LogInformation("[JOB] Updated database statistics via sp_updatestats");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[JOB] Statistics update failed (non-critical)");
        }

        try
        {
            var dbSize = await _db.Database.ExecuteSqlRawAsync(
                "SELECT 1", ct);
            _logger.LogInformation("[JOB] Database connectivity check passed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JOB] Database connectivity check failed");
            throw;
        }

        var refreshTokensCleaned = await _db.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow.AddDays(-30))
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation("[JOB] DatabaseMaintenance completed — cleaned {RefreshTokens} old refresh tokens",
            refreshTokensCleaned);
    }
}
