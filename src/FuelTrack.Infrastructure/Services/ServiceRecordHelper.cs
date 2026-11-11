using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public static class ServiceRecordHelper
{
    public static async Task<ServiceReplacementRecord?> GetLatestActiveRecordAsync(
        FuelTrackDbContext db,
        int generatorId,
        int serviceItemId,
        CancellationToken ct = default)
    {
        return await db.ServiceReplacementRecords
            .AsNoTracking()
            .Where(r => r.GeneratorId == generatorId &&
                        r.ServiceItemId == serviceItemId &&
                        r.VoidedAtUtc == null)
            .OrderByDescending(r => r.OccurredAtUtc)
            .ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);
    }

    public static async Task<bool> HasMeterResetAfterAsync(
        FuelTrackDbContext db,
        int generatorId,
        DateTime? afterOccurredAtUtc,
        CancellationToken ct = default)
    {
        var query = db.MeterReadings
            .AsNoTracking()
            .Where(r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours &&
                        r.SubjectId == generatorId &&
                        r.IsMeterReset &&
                        ReadingApprovalPolicy.OfficialStatuses.Contains(r.Status));

        if (afterOccurredAtUtc.HasValue)
        {
            query = query.Where(r => r.CapturedAtUtc > afterOccurredAtUtc.Value);
        }

        return await query.AnyAsync(ct);
    }
}
