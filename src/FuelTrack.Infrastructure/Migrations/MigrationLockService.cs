using Microsoft.Data.SqlClient;
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

    public async Task<bool> AcquireLockAsync(string resource, int lockTimeoutMs, CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("No connection string found, skipping migration lock");
            return false;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var lockResult = await AcquireLockCoreAsync(connection, resource, lockTimeoutMs, cancellationToken);
        return lockResult >= 0;
    }

    private async Task<int> AcquireLockCoreAsync(SqlConnection connection, string resource, int lockTimeoutMs, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "sp_getapplock";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@Resource", System.Data.SqlDbType.NVarChar, 255) { Value = resource });
        command.Parameters.Add(new SqlParameter("@LockMode", System.Data.SqlDbType.NVarChar, 32) { Value = "Exclusive" });
        command.Parameters.Add(new SqlParameter("@LockTimeout", System.Data.SqlDbType.Int) { Value = lockTimeoutMs });

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task ReleaseLockAsync(string resource, CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "sp_releaseapplock";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@Resource", System.Data.SqlDbType.NVarChar, 255) { Value = resource });

        await command.ExecuteScalarAsync(cancellationToken);
    }

    public async Task MigrateAsync(DbContext dbContext, int lockTimeoutMs = 60000, CancellationToken cancellationToken = default)
    {
        const string resource = "FuelTrack_Migration_Lock";

        _logger.LogInformation("Attempting to acquire migration lock ({Resource}), timeout={Timeout}ms", resource, lockTimeoutMs);

        var acquired = await AcquireLockAsync(resource, lockTimeoutMs, cancellationToken);
        if (!acquired)
        {
            _logger.LogWarning("Could not acquire migration lock, skipping migrations this instance");
            return;
        }

        try
        {
            _logger.LogInformation("Migration lock acquired, running migrations");
            await dbContext.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Migrations completed successfully");
        }
        finally
        {
            await ReleaseLockAsync(resource, cancellationToken);
            _logger.LogInformation("Migration lock released");
        }
    }
}
