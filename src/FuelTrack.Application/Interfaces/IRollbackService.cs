using FuelTrack.Application.DTOs.Rollback;

namespace FuelTrack.Application.Interfaces;

public interface IRollbackService
{
    Task<List<RollbackListDto>> GetHistoryAsync();
    Task<RollbackCheckResultDto> CheckRollbackAsync(int auditLogId);
    Task ExecuteRollbackAsync(int auditLogId, int userId);
}
