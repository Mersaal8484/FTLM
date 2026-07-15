using FuelTrack.Application.DTOs.Audits;

namespace FuelTrack.Application.Interfaces;

public interface IAuditService
{
    Task<PagedResult<AuditListDto>> GetPagedAsync(AuditFilterParams filter);
    Task<byte[]> ExportCsvAsync(AuditFilterParams filter);
}
