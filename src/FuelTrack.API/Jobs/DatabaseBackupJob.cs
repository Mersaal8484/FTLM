using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.Jobs;

public class DatabaseBackupJob
{
    private readonly FuelTrackDbContext _db;
    private readonly ILogger<DatabaseBackupJob> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseBackupJob(FuelTrackDbContext db, ILogger<DatabaseBackupJob> logger, IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[JOB] Starting DatabaseBackupJob");

        try
        {
            if (!_db.Database.IsSqlServer())
                throw new InvalidOperationException("Database backups require SQL Server.");
            var directory = _configuration["DatabaseBackup:Directory"];
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("DatabaseBackup:Directory must be configured on the SQL Server host.");

            // The directory is resolved by SQL Server, not by the API container.
            var separator = directory.Contains('\\') ? "\\" : "/";
            var path = directory.TrimEnd('/', '\\') + separator + $"fueltrack_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.bak";
            var connection = _db.Database.GetDbConnection();
            await _db.Database.OpenConnectionAsync(ct);
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandTimeout = 0;
                command.CommandText = """
                    DECLARE @sql nvarchar(max) = N'BACKUP DATABASE ' + QUOTENAME(DB_NAME()) + N' TO DISK = @path WITH COPY_ONLY, CHECKSUM';
                    EXEC sp_executesql @sql, N'@path nvarchar(4000)', @path = @backupPath;
                    RESTORE VERIFYONLY FROM DISK = @backupPath WITH CHECKSUM;
                    """;
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@backupPath";
                parameter.Value = path;
                command.Parameters.Add(parameter);
                await command.ExecuteNonQueryAsync(ct);
            }
            finally
            {
                await _db.Database.CloseConnectionAsync();
            }
            _logger.LogInformation("[JOB] SQL Server backup created and verified at {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JOB] DatabaseBackupJob failed");
            throw;
        }
    }
}
