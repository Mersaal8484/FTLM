using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FuelTrack.Infrastructure.Services;

public class DataArchivalService : IDataArchivalService
{
    private readonly FuelTrackDbContext _db;
    private readonly ILogger<DataArchivalService> _logger;

    public DataArchivalService(FuelTrackDbContext db, ILogger<DataArchivalService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> ArchiveAuditLogsAsync(int retentionDays = 90, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        _logger.LogInformation("[ARCHIVE] Archiving AuditLogs older than {Cutoff} (retention: {Days} days)", cutoff, retentionDays);

        var oldLogs = await _db.AuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.Timestamp < cutoff)
            .Take(5000)
            .ToListAsync(ct);

        if (!oldLogs.Any())
        {
            _logger.LogInformation("[ARCHIVE] No AuditLogs to archive");
            return 0;
        }

        var now = DateTime.UtcNow;
        var archives = oldLogs.Select(a => new AuditLogArchive
        {
            UserId = a.UserId,
            Action = a.Action,
            EntityName = a.EntityName,
            EntityId = a.EntityId,
            OldValues = a.OldValues,
            NewValues = a.NewValues,
            IpAddress = a.IpAddress,
            Timestamp = a.Timestamp,
            ArchivedAt = now
        }).ToList();

        await _db.AuditLogArchives.AddRangeAsync(archives, ct);
        _db.AuditLogs.RemoveRange(oldLogs);

        var saved = await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[ARCHIVE] Successfully archived {Count} AuditLogs", oldLogs.Count);
        return oldLogs.Count;
    }

    public async Task<int> ArchiveDailyReadingsAsync(int retentionDays = 365, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        _logger.LogInformation("[ARCHIVE] Archiving DailyReadings older than {Cutoff} (retention: {Days} days)", cutoff, retentionDays);

        var oldReadings = await _db.DailyReadings
            .IgnoreQueryFilters()
            .Where(r => r.ReadingDate < cutoff && (r.DeletedAt != null || r.ReadingStatusLookupId == 2)) // Approved or deleted
            .Take(5000)
            .ToListAsync(ct);

        if (!oldReadings.Any())
        {
            _logger.LogInformation("[ARCHIVE] No DailyReadings to archive");
            return 0;
        }

        var now = DateTime.UtcNow;
        var archives = oldReadings.Select(r => new DailyReadingArchive
        {
            GeneratorId = r.GeneratorId,
            ReadingDate = r.ReadingDate,
            PrevEnergyKWh = r.PrevEnergyKWh,
            CurrentEnergyKWh = r.CurrentEnergyKWh,
            PrevRunningHours = r.PrevRunningHours,
            CurrentRunningHours = r.CurrentRunningHours,
            FuelLookupId = r.FuelLookupId,
            StandardFuelRate = r.StandardFuelRate,
            OilLookupId = r.OilLookupId,
            OilUsedLiters = r.OilUsedLiters,
            ReadingStatusLookupId = r.ReadingStatusLookupId,
            ArchivedAt = now
        }).ToList();

        await _db.DailyReadingArchives.AddRangeAsync(archives, ct);
        _db.DailyReadings.RemoveRange(oldReadings);

        var saved = await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[ARCHIVE] Successfully archived {Count} DailyReadings", oldReadings.Count);
        return oldReadings.Count;
    }
}
