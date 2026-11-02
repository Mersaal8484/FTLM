using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.RBAC;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<Role> _roleManager;
    private readonly UserManager<User> _userManager;
    private readonly FuelTrackDbContext _context;
    private readonly ICacheService _cache;

    public RoleService(
        RoleManager<Role> roleManager,
        UserManager<User> userManager,
        FuelTrackDbContext context,
        ICacheService cache)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _context = context;
        _cache = cache;
    }

    public async Task<List<RoleDto>> GetAllAsync()
    {
        var cached = await _cache.GetAsync<List<RoleDto>>(CacheKeys.RolesAll);
        if (cached is not null) return cached;

        var roles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .ToListAsync();

        var roleIds = roles.Select(r => r.Id).ToList();
        var permissionCounts = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.IsGranted)
            .GroupBy(rp => rp.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToListAsync();
        var countDict = permissionCounts.ToDictionary(x => x.RoleId, x => x.Count);

        var result = roles.Select(r => new RoleDto(
            r.Id, r.Name!, r.Key, r.Description,
            r.IsActive, r.CreatedAt,
            countDict.GetValueOrDefault(r.Id, 0)))
            .ToList();

        await _cache.SetAsync(CacheKeys.RolesAll, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<RoleDto?> GetByIdAsync(int id)
    {
        var cached = await _cache.GetAsync<RoleDto>(CacheKeys.RoleById(id));
        if (cached is not null) return cached;

        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) return null;

        var permCount = await _context.RolePermissions
            .CountAsync(rp => rp.RoleId == id && rp.IsGranted);

        var dto = new RoleDto(
            role.Id, role.Name!, role.Key, role.Description,
            role.IsActive, role.CreatedAt, permCount);

        await _cache.SetAsync(CacheKeys.RoleById(id), dto, TimeSpan.FromMinutes(5));
        return dto;
    }

    public async Task<Result<RoleDto>> CreateAsync(CreateRoleDto dto, int createdBy)
    {
        var exists = await _roleManager.Roles.AnyAsync(r => r.Name == dto.Name || r.Key == dto.Key);
        if (exists)
            return Result<RoleDto>.Failure("A role with this name or key already exists.");

        var role = new Role
        {
            Name = dto.Name,
            Key = dto.Key,
            Description = dto.Description,
            IsActive = dto.IsActive ?? true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = createdBy
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return Result<RoleDto>.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));

        if (dto.PermissionIds is not null && dto.PermissionIds.Count > 0)
        {
            var rolePermissions = dto.PermissionIds.Select(ppId => new RolePermission
            {
                RoleId = role.Id,
                PagePermissionId = ppId,
                IsGranted = true
            }).ToList();
            _context.RolePermissions.AddRange(rolePermissions);
            await _context.SaveChangesAsync();
        }

        await _cache.RemoveAsync(CacheKeys.RolesAll);
        PermissionCache.ClearAll();

        return Result<RoleDto>.Success(new RoleDto(
            role.Id, role.Name!, role.Key, role.Description,
            role.IsActive, role.CreatedAt, dto.PermissionIds?.Count ?? 0));
    }

    public async Task<Result<RoleDto>> UpdateAsync(int id, UpdateRoleDto dto, int updatedBy)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return Result<RoleDto>.NotFound($"Role {id} not found.");

        if (dto.Name != null) role.Name = dto.Name;
        if (dto.Key != null) role.Key = dto.Key;
        if (dto.Description != null) role.Description = dto.Description;
        if (dto.IsActive.HasValue) role.IsActive = dto.IsActive.Value;
        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedBy = updatedBy;

        await _roleManager.UpdateAsync(role);

        await _cache.RemoveAsync(CacheKeys.RolesAll);
        await _cache.RemoveAsync(CacheKeys.RoleById(id));
        PermissionCache.ClearAll();

        return Result<RoleDto>.Success(new RoleDto(
            role.Id, role.Name!, role.Key, role.Description,
            role.IsActive, role.CreatedAt));
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return Result.NotFound($"Role {id} not found.");

        var protectedKeys = new[] { "admin", "supervisor", "operator" };
        if (protectedKeys.Contains(role.Key))
            return Result.Failure($"Cannot delete protected role '{role.Key}'.");

        var existingPerms = await _context.RolePermissions
            .Where(rp => rp.RoleId == id)
            .ToListAsync();
        _context.RolePermissions.RemoveRange(existingPerms);
        await _context.SaveChangesAsync();

        role.IsActive = false;
        await _roleManager.UpdateAsync(role);

        await _cache.RemoveAsync(CacheKeys.RolesAll);
        await _cache.RemoveAsync(CacheKeys.RoleById(id));
        PermissionCache.ClearAll();
        return Result.Success();
    }

    public async Task<Result<RolePermissionsDto>> GetRolePermissionsAsync(int roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return Result<RolePermissionsDto>.NotFound($"Role {roleId} not found.");

        var details = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Include(rp => rp.PagePermission)
                .ThenInclude(pp => pp.ModuleDefinition)
            .Select(rp => new RolePermissionDetail(
                rp.PagePermissionId,
                rp.PagePermission.ModuleDefinition.Key,
                rp.PagePermission.ModuleDefinition.NameEn,
                rp.PagePermission.ActionKey,
                rp.PagePermission.NameEn,
                rp.IsGranted))
            .ToListAsync();

        return Result<RolePermissionsDto>.Success(
            new RolePermissionsDto(roleId, role.Name!, details));
    }

    public async Task<Result> SaveRolePermissionsAsync(SaveRolePermissionsRequest request, int updatedBy)
    {
        var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
        if (role is null)
            return Result.NotFound($"Role {request.RoleId} not found.");

        var existing = await _context.RolePermissions
            .Where(rp => rp.RoleId == request.RoleId)
            .ToListAsync();
        _context.RolePermissions.RemoveRange(existing);

        var newPerms = request.Permissions.Select(p => new RolePermission
        {
            RoleId = request.RoleId,
            PagePermissionId = p.PagePermissionId,
            IsGranted = p.IsGranted
        }).ToList();
        _context.RolePermissions.AddRange(newPerms);

        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedBy = updatedBy;
        await _roleManager.UpdateAsync(role);
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(CacheKeys.RolesAll);
        await _cache.RemoveAsync(CacheKeys.RoleById(request.RoleId));
        PermissionCache.ClearAll();
        return Result.Success();
    }

    public async Task<List<ModuleDto>> GetAllModulesAsync()
    {
        var cached = await _cache.GetAsync<List<ModuleDto>>("modules:all");
        if (cached is not null) return cached;

        var modules = await _context.ModuleDefinitions
            .Where(m => m.IsActive)
            .Include(m => m.PagePermissions.Where(pp => pp.IsActive))
            .OrderBy(m => m.SortOrder)
            .Select(m => new ModuleDto(
                m.Id, m.Key, m.NameEn, m.NameAr, m.Route, m.Icon,
                m.ParentKey, (int)m.Template, (int)m.ScopeType,
                m.CustomActions,
                m.SortOrder, m.IsVisible, m.IsActive,
                m.SupportsAuditLog, m.SupportsGlobalSearch,
                m.PagePermissions.Select(pp => new PagePermissionDto(
                    pp.Id, pp.ModuleDefinitionId, pp.ActionKey,
                    pp.NameEn, pp.NameAr, pp.SortOrder, pp.IsActive
                )).ToList()))
            .ToListAsync();

        await _cache.SetAsync("modules:all", modules, TimeSpan.FromMinutes(10));
        return modules;
    }

    public async Task<bool> HasPermissionAsync(int userId, string moduleKey, string actionKey)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        var roleNames = await _userManager.GetRolesAsync(user);
        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null) continue;

            var hasPerm = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == role.Id
                    && rp.IsGranted
                    && rp.PagePermission.ModuleDefinition.Key == moduleKey
                    && rp.PagePermission.ActionKey == actionKey);
            if (hasPerm) return true;
        }
        return false;
    }

    public async Task InitAsync()
    {
        var existingAdmin = await _roleManager.FindByNameAsync("Admin");
        var allPermissions = await _context.PagePermissions
            .Include(pp => pp.ModuleDefinition)
            .ToListAsync();

        if (existingAdmin == null)
        {
            var adminRole = new Role
            {
                Name = "Admin",
                Key = "admin",
                Description = "Full system administrator with all permissions",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _roleManager.CreateAsync(adminRole);

            var adminPerms = allPermissions.Select(pp => new RolePermission
            {
                RoleId = adminRole.Id,
                PagePermissionId = pp.Id,
                IsGranted = true
            }).ToList();
            _context.RolePermissions.AddRange(adminPerms);
            await _context.SaveChangesAsync();
        }

        var existingSystemOwner = await _roleManager.FindByNameAsync("SystemOwner");
        if (existingSystemOwner == null)
        {
            var systemOwnerRole = new Role
            {
                Name = "SystemOwner",
                Key = "systemowner",
                Description = "System owner with full unrestricted access (bypasses all scope and permission checks)",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _roleManager.CreateAsync(systemOwnerRole);

            var ownerPerms = allPermissions.Select(pp => new RolePermission
            {
                RoleId = systemOwnerRole.Id,
                PagePermissionId = pp.Id,
                IsGranted = true
            }).ToList();
            _context.RolePermissions.AddRange(ownerPerms);
            await _context.SaveChangesAsync();
        }

        var existingOperator = await _roleManager.FindByNameAsync("Operator");
        var viewOnlyPerms = allPermissions
            .Where(pp => pp.ActionKey == "view" || pp.ActionKey == "read")
            .ToList();

        if (existingOperator == null)
        {
            var operatorRole = new Role
            {
                Name = "Operator",
                Key = "operator",
                Description = "Basic operator with view-only access",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _roleManager.CreateAsync(operatorRole);

            var operatorPerms = viewOnlyPerms.Select(pp => new RolePermission
            {
                RoleId = operatorRole.Id,
                PagePermissionId = pp.Id,
                IsGranted = true
            }).ToList();
            _context.RolePermissions.AddRange(operatorPerms);
            await _context.SaveChangesAsync();
        }
    }
}
