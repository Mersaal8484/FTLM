using FuelTrack.Application.DTOs.Branches;

namespace FuelTrack.Application.Interfaces;

public interface IBranchService
{
    Task<BranchDetailDto> CreateAsync(CreateBranchDto dto, int userId);
    Task<BranchDetailDto> UpdateAsync(int id, UpdateBranchDto dto, int userId);
    Task DeleteAsync(int id);
    Task<BranchDetailDto?> GetByIdAsync(int id);
    Task<List<BranchListDto>> GetAllAsync();
}
