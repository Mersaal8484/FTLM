namespace FuelTrack.Application.Interfaces;

public interface IDataArchivalService
{
    Task<int> ArchiveAuditLogsAsync(int retentionDays = 90, CancellationToken ct = default);
    Task<int> ArchiveDailyReadingsAsync(int retentionDays = 365, CancellationToken ct = default);
}
