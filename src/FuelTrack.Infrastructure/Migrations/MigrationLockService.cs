using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FuelTrack.Infrastructure.Migrations;

public class MigrationLockService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MigrationLockService> _logger;

    public MigrationLockService(IConfiguration configuration, ILogger<MigrationLockService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task MigrateAsync(DbContext dbContext, int lockTimeoutMs = 60000, CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsRelational())
        {
            _logger.LogInformation("Database provider is not relational, skipping EF migrations");
            return;
        }

        _logger.LogInformation("Applying EF Core migrations...");
        await dbContext.Database.MigrateAsync(cancellationToken);
        _logger.LogInformation("Migrations completed successfully");
    }
}
