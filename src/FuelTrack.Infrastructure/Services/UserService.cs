using FuelTrack.Application;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.RBAC;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly FuelTrackDbContext _db;

    public UserService(UserManager<User> userManager, FuelTrackDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        return await MapWithRolesBatchAsync(users);
    }

    public async Task<UserDetailDto?> GetByIdAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return null;

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var scopes = await _db.UserScopes
            .Where(s => s.UserId == id)
            .Select(s => new UserScopeDto(s.BranchId, s.StationId, s.SubStationId))
            .ToListAsync();

        return new UserDetailDto(
            user.Id, user.UserName!, user.FullName, user.Email,
            user.PhoneNumber, user.IsActive, user.CreatedAt,
            user.UpdatedAt, roles, scopes);
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserDto dto, int createdBy)
    {
        var user = new User
        {
            UserName = dto.UserName,
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = createdBy
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return Result<UserDto>.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));

        if (dto.Roles?.Count > 0)
        {
            var roleNames = await _db.Roles
                .Where(r => dto.Roles.Contains(r.Key))
                .Select(r => r.Name!)
                .ToListAsync();
            if (roleNames.Count > 0)
                await _userManager.AddToRolesAsync(user, roleNames);
        }

        if (dto.Scopes?.Count > 0)
        {
            var scopeResult = await AssignScopesAsync(user.Id, dto.Scopes);
            if (!scopeResult.IsSuccess)
            {
                await _userManager.DeleteAsync(user);
                return Result<UserDto>.Failure(scopeResult.Error!);
            }
        }

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        return Result<UserDto>.Success(MapToDto(user, roles));
    }

    public async Task<Result<UserDto>> UpdateAsync(int id, UpdateUserDto dto, int updatedBy)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Result<UserDto>.NotFound($"User {id} not found.");

        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.Email != null) user.Email = dto.Email;
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = updatedBy;

        await _userManager.UpdateAsync(user);
        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        return Result<UserDto>.Success(MapToDto(user, roles));
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Result.NotFound($"User {id} not found.");
        user.IsActive = false;
        await _userManager.UpdateAsync(user);
        return Result.Success();
    }

    public async Task<Result> AssignRolesAsync(int userId, List<string> roleKeys)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.NotFound($"User {userId} not found.");

        // Resolve role keys to Identity role names
        var roleNames = await _db.Roles
            .Where(r => roleKeys.Contains(r.Key))
            .Select(r => r.Name!)
            .ToListAsync();

        if (roleNames.Count != roleKeys.Count)
        {
            var missing = roleKeys.Where(k => !roleNames.Contains(k));
            return Result.Failure($"Roles not found: {string.Join(", ", missing)}");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRolesAsync(user, roleNames);
        return Result.Success();
    }

    public async Task<Result<List<string>>> GetUserRolesAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result<List<string>>.NotFound($"User {userId} not found.");
        return Result<List<string>>.Success((await _userManager.GetRolesAsync(user)).ToList());
    }

    public async Task<Result> ActivateAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Result.NotFound($"User {id} not found.");
        user.IsActive = true;
        await _userManager.UpdateAsync(user);
        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Result.NotFound($"User {id} not found.");
        user.IsActive = false;
        await _userManager.UpdateAsync(user);
        return Result.Success();
    }

    public async Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize)
    {
        var query = _userManager.Users.OrderBy(u => u.UserName);
        var totalCount = await query.CountAsync();
        var users = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var result = await MapWithRolesBatchAsync(users);

        return new PagedResult<UserDto>
        {
            Items = result,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Result> ResetPasswordAsync(int id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Result.NotFound($"User {id} not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            return Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
        return Result.Success();
    }

    public async Task<Result> AssignScopesAsync(int userId, List<UserScopeDto> scopes)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.NotFound($"User {userId} not found.");

        if (scopes == null || scopes.Count == 0)
        {
            var existingEmpty = await _db.UserScopes.Where(x => x.UserId == userId).ToListAsync();
            _db.UserScopes.RemoveRange(existingEmpty);
            await _db.SaveChangesAsync();
            return Result.Success();
        }

        var globalCount = scopes.Count(s => s.BranchId == null && s.StationId == null && s.SubStationId == null);
        if (globalCount > 1)
            return Result.Failure("Only one global (All) scope is allowed.", 400);

        if (globalCount == 1 && scopes.Count > 1)
            return Result.Failure("Global scope covers all access. Remove other scopes.", 400);

        var seen = new HashSet<string>();
        foreach (var scope in scopes)
        {
            var key = $"{scope.BranchId}|{scope.StationId}|{scope.SubStationId}";
            if (!seen.Add(key))
                return Result.Failure($"Duplicate scope detected: {key}.", 400);

            if (scope.SubStationId.HasValue && !scope.StationId.HasValue)
                return Result.Failure("SubStationId requires a StationId.", 400);

            if (scope.StationId.HasValue && !scope.BranchId.HasValue)
                return Result.Failure("StationId requires a BranchId.", 400);
        }

        var existing = await _db.UserScopes.Where(x => x.UserId == userId).ToListAsync();
        _db.UserScopes.RemoveRange(existing);

        foreach (var scope in scopes)
        {
            _db.UserScopes.Add(new UserScope
            {
                UserId = userId,
                BranchId = scope.BranchId,
                StationId = scope.StationId,
                SubStationId = scope.SubStationId
            });
        }

        await _db.SaveChangesAsync();
        return Result.Success();
    }

    private async Task<List<UserDto>> MapWithRolesBatchAsync(List<User> users)
    {
        var userIds = users.Select(u => u.Id).ToList();
        var userRoles = await _db.Set<IdentityUserRole<int>>()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_db.Set<Role>(),
                  ur => ur.RoleId,
                  r => r.Id,
                  (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync();
        var rolesByUser = userRoles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name!).ToList());

        return users.Select(u => MapToDto(u, rolesByUser.GetValueOrDefault(u.Id, []))).ToList();
    }

    private static UserDto MapToDto(User user, List<string> roles) => new(
        user.Id, user.UserName!, user.FullName, user.Email,
        user.PhoneNumber, user.IsActive, user.CreatedAt, roles);
}
