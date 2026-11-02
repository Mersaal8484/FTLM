using FuelTrack.Application.DTOs.FuelMovements;

namespace FuelTrack.Application.Interfaces;

public interface IFuelMovementService
{
    Task<FuelMovementDetailDto> CreateAsync(CreateFuelMovementDto dto, int userId);
    Task<FuelMovementDetailDto> UpdateAsync(int id, UpdateFuelMovementDto dto, int userId);
    Task<FuelMovementDetailDto?> GetByIdAsync(int id);
    Task<List<FuelMovementListDto>> GetAllAsync();
    Task<PagedResult<FuelMovementListDto>> GetPagedAsync(int page, int pageSize, string? search = null, string? status = null);
    Task<List<FuelMovementListDto>> GetBySubStationAsync(int subStationId);
    Task<PagedResult<FuelMovementListDto>> GetPagedBySubStationAsync(int subStationId, int page, int pageSize, string? search = null, string? status = null);
    Task<List<FuelMovementListDto>> GetByFuelTankAsync(int fuelTankId);
    Task<PagedResult<FuelMovementListDto>> GetPagedByFuelTankAsync(int fuelTankId, int page, int pageSize, string? search = null, string? status = null);
    Task<FuelMovementDetailDto> SubmitForApprovalAsync(int id, int userId);
    Task<FuelMovementDetailDto> ApproveMovementAsync(int id, int authorizedBy);
    Task<FuelMovementDetailDto> RejectMovementAsync(int id, int rejectedById, string? rejectionReason = null);
    Task DeleteAsync(int id);
    Task<FuelMovementDetailDto?> GetLastBySubStationAsync(int subStationId);
}
