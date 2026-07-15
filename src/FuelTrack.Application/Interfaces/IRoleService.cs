using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.RBAC;

namespace FuelTrack.Application.Interfaces;

public interface IRoleService
{
    Task<List<RoleDto>> GetAllAsync();
    Task<RoleDto?> GetByIdAsync(int id);
    Task<Result<RoleDto>> CreateAsync(CreateRoleDto dto, int createdBy);
    Task<Result<RoleDto>> UpdateAsync(int id, UpdateRoleDto dto, int updatedBy);
    Task<Result> DeleteAsync(int id);
    Task<Result<RolePermissionsDto>> GetRolePermissionsAsync(int roleId);
    Task<Result> SaveRolePermissionsAsync(SaveRolePermissionsRequest request, int updatedBy);
    Task<List<ModuleDto>> GetAllModulesAsync();
    Task<bool> HasPermissionAsync(int userId, string moduleKey, string actionKey);
    Task InitAsync();
}
