using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.RBAC;

namespace FuelTrack.Application.Interfaces;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDetailDto?> GetByIdAsync(int id);
    Task<Result<UserDto>> CreateAsync(CreateUserDto dto, int createdBy);
    Task<Result<UserDto>> UpdateAsync(int id, UpdateUserDto dto, int updatedBy);
    Task<Result> DeleteAsync(int id);
    Task<Result> AssignRolesAsync(int userId, List<string> roles);
    Task<Result<List<string>>> GetUserRolesAsync(int userId);
    Task<Result> ActivateAsync(int id);
    Task<Result> DeactivateAsync(int id);
    Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize);
    Task<Result> ResetPasswordAsync(int id, string newPassword);
    Task<Result> AssignScopesAsync(int userId, List<UserScopeDto> scopes);
}
