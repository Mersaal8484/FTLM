namespace FuelTrack.Application.DTOs.RBAC;

public record ModuleDto(
    int Id,
    string Key,
    string NameEn,
    string NameAr,
    string Route,
    string Icon,
    string? ParentKey,
    int Template,
    int ScopeType,
    string? CustomActions,
    int SortOrder,
    bool IsVisible,
    bool IsActive,
    bool SupportsAuditLog,
    bool SupportsGlobalSearch,
    List<PagePermissionDto>? PagePermissions = null);

public record PagePermissionDto(
    int Id,
    int ModuleDefinitionId,
    string ActionKey,
    string NameEn,
    string NameAr,
    int SortOrder,
    bool IsActive);

public record SaveRolePermissionsRequest(
    int RoleId,
    List<RolePermissionEntry> Permissions);

public record RolePermissionEntry(
    int PagePermissionId,
    bool IsGranted);

public record RolePermissionsDto(
    int RoleId,
    string RoleName,
    List<RolePermissionDetail> Permissions);

public record RolePermissionDetail(
    int PagePermissionId,
    string ModuleKey,
    string ModuleName,
    string ActionKey,
    string ActionName,
    bool IsGranted);

public record UserPermissionsDto(
    int UserId,
    string UserName,
    List<string> Roles,
    Dictionary<string, Dictionary<string, bool>> Permissions);

public record PermissionDto(
    string Name,
    string DisplayName,
    string Group);
