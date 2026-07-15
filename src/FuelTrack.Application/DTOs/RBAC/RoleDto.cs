namespace FuelTrack.Application.DTOs.RBAC;

    public record RoleDto(
        int Id,
        string Name,
        string? Key,
        string? Description,
        bool IsActive,
        DateTime CreatedAt,
        int PermissionCount = 0);

    public record CreateRoleDto(
        string Name,
        string? Key,
        string? Description,
        List<int>? PermissionIds,
        bool? IsActive);

    public record UpdateRoleDto(
        string? Name,
        string? Key,
        string? Description,
        List<int>? PermissionIds,
        bool? IsActive);
