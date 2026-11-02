using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Warehouse;

namespace FuelTrack.Application.Interfaces;

public interface IDispatchRequestService
{
    Task<Result<DispatchRequestDetailDto>> CreateAsync(CreateDispatchRequestDto dto, int userId, CancellationToken ct = default);
    Task<Result<DispatchRequestDetailDto>> UpdateAsync(int id, UpdateDispatchRequestDto dto, int userId, CancellationToken ct = default);
    Task<Result<DispatchRequestDetailDto>> ApproveAsync(int id, ApproveDispatchRequestDto dto, int userId, CancellationToken ct = default);
    Task<Result<DispatchRequestDetailDto>> RejectAsync(int id, string reason, int userId, CancellationToken ct = default);
    Task<Result<DispatchRequestDetailDto>> IssueAsync(int id, int userId, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<DispatchRequestDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<DispatchRequestListDto>> GetBySubStationAsync(int subStationId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<DispatchRequestListDto>> GetByStationAsync(int stationId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<DispatchRequestListDto>> GetAllAsync(int page, int pageSize, int? status, CancellationToken ct = default);
    Task<List<DispatchApprovalItemDto>> GetApprovalDataAsync(int id, CancellationToken ct = default);
}
