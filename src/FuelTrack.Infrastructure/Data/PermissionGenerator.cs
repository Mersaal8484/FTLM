using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Data;

/// <summary>
/// Auto-generates PagePermissions from ModuleDefinitions.
/// This is the core of the permission generation system.
/// </summary>
public class PermissionGenerator
{
    private readonly FuelTrackDbContext _db;

    public PermissionGenerator(FuelTrackDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Generates all permissions for all modules.
    /// Call this on startup and when modules change.
    /// </summary>
    public async Task GenerateAllPermissionsAsync()
    {
        var modules = await _db.ModuleDefinitions
            .Where(m => m.IsActive)
            .ToListAsync();

        foreach (var module in modules)
        {
            await GeneratePermissionsForModuleAsync(module);
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Generates permissions for a single module based on its template.
    /// </summary>
    public async Task GeneratePermissionsForModuleAsync(ModuleDefinition module)
    {
        // Get existing permissions for this module
        var existingPermissions = await _db.PagePermissions
            .Where(p => p.ModuleDefinitionId == module.Id && p.IsActive)
            .ToListAsync();

        var existingKeys = existingPermissions.Select(p => p.ActionKey).ToHashSet();

        // Generate permissions based on template
        var requiredPermissions = GetRequiredPermissions(module);

        foreach (var perm in requiredPermissions)
        {
            if (!existingKeys.Contains(perm.Key))
            {
                _db.PagePermissions.Add(new PagePermission
                {
                    ModuleDefinitionId = module.Id,
                    ActionKey = perm.Key,
                    NameEn = perm.NameEn,
                    NameAr = perm.NameAr
                });
            }
        }

        // Remove permissions that are no longer required
        var requiredKeys = requiredPermissions.Select(p => p.Key).ToHashSet();
        foreach (var existing in existingPermissions)
        {
            if (!requiredKeys.Contains(existing.ActionKey))
            {
                existing.IsActive = false;
            }
        }
    }

    /// <summary>
    /// Gets the required permissions for a module based on its template.
    /// </summary>
    private List<PermissionDefinition> GetRequiredPermissions(ModuleDefinition module)
    {
        var permissions = new List<PermissionDefinition>();

        // Base permissions by template
        switch (module.Template)
        {
            case ModuleTemplate.CRUD:
                permissions.Add(new("view", "View", "عرض"));
                permissions.Add(new("create", "Create", "إنشاء"));
                permissions.Add(new("update", "Update", "تعديل"));
                permissions.Add(new("delete", "Delete", "حذف"));
                break;

            case ModuleTemplate.Approval:
                permissions.Add(new("view", "View", "عرض"));
                permissions.Add(new("create", "Create", "إنشاء"));
                permissions.Add(new("update", "Update", "تعديل"));
                permissions.Add(new("delete", "Delete", "حذف"));
                permissions.Add(new("approve", "Approve", "موافقة"));
                permissions.Add(new("reject", "Reject", "رفض"));
                break;

            case ModuleTemplate.Report:
                permissions.Add(new("view", "View", "عرض"));
                permissions.Add(new("print", "Print", "طباعة"));
                permissions.Add(new("export", "Export", "تصدير"));
                break;

            case ModuleTemplate.Dashboard:
                permissions.Add(new("view", "View", "عرض"));
                break;

            case ModuleTemplate.Settings:
                permissions.Add(new("view", "View", "عرض"));
                permissions.Add(new("update", "Update", "تعديل"));
                break;

            case ModuleTemplate.ReadOnly:
                permissions.Add(new("view", "View", "عرض"));
                break;
        }

        // Add custom actions from module
        if (!string.IsNullOrEmpty(module.CustomActions))
        {
            var customActions = module.CustomActions.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var action in customActions)
            {
                var actionTrimmed = action.Trim();
                if (!permissions.Any(p => p.Key == actionTrimmed))
                {
                    permissions.Add(new(actionTrimmed, 
                        char.ToUpper(actionTrimmed[0]) + actionTrimmed[1..], 
                        actionTrimmed));
                }
            }
        }

        return permissions;
    }

    /// <summary>
    /// Assigns all permissions to a role (used for System Owner).
    /// </summary>
    public async Task AssignAllPermissionsToRoleAsync(int roleId)
    {
        var allPermissions = await _db.PagePermissions
            .Where(p => p.IsActive)
            .ToListAsync();

        var existingRolePerms = await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PagePermissionId)
            .ToListAsync();

        var existingSet = existingRolePerms.ToHashSet();

        foreach (var perm in allPermissions)
        {
            if (!existingSet.Contains(perm.Id))
            {
                _db.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PagePermissionId = perm.Id,
                    IsGranted = true
                });
            }
        }
    }

    private record PermissionDefinition(string Key, string NameEn, string NameAr);
}
