using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class ArchiveQueryService
{
    private readonly FuelTrackDbContext _db;

    public ArchiveQueryService(FuelTrackDbContext db)
    {
        _db = db;
    }

    public async Task<List<DailyReadingArchive>> GetArchivedReadingsAsync(
        DateTime from, DateTime to, int? generatorId = null, CancellationToken ct = default)
    {
        var query = _db.DailyReadingArchives
            .Where(a => a.ArchivedAt >= from && a.ArchivedAt <= to);

        if (generatorId.HasValue)
            query = query.Where(a => a.GeneratorId == generatorId.Value);

        return await query.OrderByDescending(a => a.ArchivedAt).ToListAsync(ct);
    }

    public async Task<List<AuditLogArchive>> GetArchivedAuditLogsAsync(
        DateTime from, DateTime to, string? entityName = null, CancellationToken ct = default)
    {
        var query = _db.AuditLogArchives
            .Where(a => a.ArchivedAt >= from && a.ArchivedAt <= to);

        if (!string.IsNullOrEmpty(entityName))
            query = query.Where(a => a.EntityName == entityName);

        return await query.OrderByDescending(a => a.ArchivedAt).ToListAsync(ct);
    }

    public async Task<int> ArchiveOldDailyReadingsAsync(DateTime olderThan, CancellationToken ct = default)
    {
        var toArchive = await _db.DailyReadings
            .Where(r => r.CreatedAt < olderThan)
            .ToListAsync(ct);

        if (toArchive.Count == 0) return 0;

        var archives = toArchive.Select(r => new DailyReadingArchive
        {
            Id = r.Id,
            GeneratorId = r.GeneratorId,
            ReadingDate = r.ReadingDate,
            PrevEnergyKWh = (double)r.PrevEnergyKWh,
            CurrentEnergyKWh = (double)r.CurrentEnergyKWh,
            PrevRunningHours = (double)r.PrevRunningHours,
            CurrentRunningHours = (double)r.CurrentRunningHours,
            FuelLookupId = r.FuelLookupId,
            StandardFuelRate = r.StandardFuelRate,
            OilLookupId = r.OilLookupId,
            OilUsedLiters = r.OilUsedLiters,
            ReadingStatusLookupId = r.ReadingStatusLookupId,
            ArchivedAt = DateTime.UtcNow
        }).ToList();

        _db.DailyReadingArchives.AddRange(archives);
        _db.DailyReadings.RemoveRange(toArchive);
        await _db.SaveChangesAsync(ct);

        return archives.Count;
    }

    public async Task<int> ArchiveOldAuditLogsAsync(DateTime olderThan, CancellationToken ct = default)
    {
        var toArchive = await _db.AuditLogs
            .Where(a => a.Timestamp < olderThan)
            .ToListAsync(ct);

        if (toArchive.Count == 0) return 0;

        var archives = toArchive.Select(a => new AuditLogArchive
        {
            Id = a.Id,
            UserId = a.UserId,
            Action = a.Action,
            EntityName = a.EntityName,
            EntityId = a.EntityId,
            OldValues = a.OldValues,
            NewValues = a.NewValues,
            IpAddress = a.IpAddress,
            Timestamp = a.Timestamp,
            ArchivedAt = DateTime.UtcNow
        }).ToList();

        _db.AuditLogArchives.AddRange(archives);
        _db.AuditLogs.RemoveRange(toArchive);
        await _db.SaveChangesAsync(ct);

        return archives.Count;
    }
}
