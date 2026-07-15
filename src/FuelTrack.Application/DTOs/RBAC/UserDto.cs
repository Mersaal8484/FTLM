namespace FuelTrack.Application.DTOs.RBAC;

public record UserDto(
    int Id,
    string UserName,
    string FullName,
    string? Email,
    string? PhoneNumber,
    bool IsActive,
    DateTime CreatedAt,
    List<string> Roles);

public record CreateUserDto(
    string UserName,
    string FullName,
    string Email,
    string? PhoneNumber,
    string Password,
    List<string>? Roles,
    List<UserScopeDto>? Scopes);

public record UpdateUserDto(
    string? FullName,
    string? Email,
    string? PhoneNumber,
    bool? IsActive);

public record UserDetailDto(
    int Id,
    string UserName,
    string FullName,
    string? Email,
    string? PhoneNumber,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<string> Roles,
    List<UserScopeDto> Scopes);

public record UserScopeDto(
    int? BranchId,
    int? StationId,
    int? SubStationId);

public record AssignScopesDto(List<UserScopeDto> Scopes);
