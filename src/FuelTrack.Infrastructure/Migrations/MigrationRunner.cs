using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FuelTrack.Infrastructure.Migrations;

public class MigrationRunner
{
    private readonly MigrationLockService _lockService;
    private readonly FuelTrackDbContext _dbContext;
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner(
        MigrationLockService lockService,
        FuelTrackDbContext dbContext,
        ILogger<MigrationRunner> logger)
    {
        _lockService = lockService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task RunAsync(int lockTimeoutMs = 60000, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MigrationRunner: starting migration with lock");

        try
        {
            await _lockService.MigrateAsync(_dbContext, lockTimeoutMs, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MigrationRunner: migration failed");
            throw;
        }
    }
}
