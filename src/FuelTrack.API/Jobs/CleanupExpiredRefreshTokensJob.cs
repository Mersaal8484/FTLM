using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.Jobs;

public class CleanupExpiredRefreshTokensJob
{
    private readonly FuelTrackDbContext _db;
    private readonly ILogger<CleanupExpiredRefreshTokensJob> _logger;

    public CleanupExpiredRefreshTokensJob(FuelTrackDbContext db, ILogger<CleanupExpiredRefreshTokensJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[JOB] Starting CleanupExpiredRefreshTokens");

        var now = DateTime.UtcNow;

        var revokedDeleted = await _db.RefreshTokens
            .Where(rt => rt.IsRevoked && rt.RevokedAt != null && rt.RevokedAt < now.AddDays(-30))
            .ExecuteDeleteAsync(ct);

        var expiredDeleted = await _db.RefreshTokens
            .Where(rt => rt.ExpiresAt < now.AddDays(-7))
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation("[JOB] Removed {Revoked} revoked (>30d) and {Expired} expired (>7d) refresh tokens",
            revokedDeleted, expiredDeleted);
    }
}
