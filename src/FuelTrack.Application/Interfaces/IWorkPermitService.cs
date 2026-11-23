using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Interfaces;

public interface IWorkPermitService
{
    Task<Result<WorkPermitDetailDto>> CreateAsync(CreateWorkPermitDto dto, int userId, CancellationToken ct = default);
    Task<Result<WorkPermitDetailDto>> UpdateAsync(int id, UpdateWorkPermitDto dto, int userId, CancellationToken ct = default);
    Task<Result<WorkPermitDetailDto>> UpdateStatusAsync(int id, UpdateWorkPermitStatusDto dto, int userId, CancellationToken ct = default);
    Task<Result<WorkPermitDetailDto>> ApproveAsync(int id, string? notes, int userId, CancellationToken ct = default);
    Task<Result<WorkPermitDetailDto>> RejectAsync(int id, string reason, int userId, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<WorkPermitDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<WorkPermitSheetDto?> GetSheetAsync(int id, CancellationToken ct = default);
    Task<PagedResult<WorkPermitListDto>> GetBySubStationAsync(int subStationId, int page, int pageSize, int? status, CancellationToken ct = default);
    Task<PagedResult<WorkPermitListDto>> GetByGeneratorAsync(int generatorId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<WorkPermitListDto>> GetAllAsync(int page, int pageSize, int? status, int? priority, int? subStationId, CancellationToken ct = default);
    Task<WorkTaskDto> CompleteTaskAsync(int permitId, int taskId, int userId, CancellationToken ct = default);
    Task<MaintenanceLogDto> AddLogAsync(int permitId, string action, string? notes, int userId, CancellationToken ct = default);
    Task<string> GeneratePermitNumberAsync(int subStationId, CancellationToken ct = default);
}
