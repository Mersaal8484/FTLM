using System.Security.Claims;
using FuelTrack.Application.Common;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.Middleware;

/// <summary>
/// Maps controller names to module keys and HTTP methods to actions.
/// Used by PermissionEnforcementMiddleware to resolve permission requirements.
/// </summary>
public static class PermissionMapping
{
    private static readonly Dictionary<string, string> ControllerToModule = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Users"] = "users",
        ["Roles"] = "roles",
        ["Branches"] = "branches",
        ["Stations"] = "stations",
        ["SubStations"] = "sub_stations",
        ["Generators"] = "generators",
        ["FuelTanks"] = "fuel_tanks",
        ["OilTanks"] = "oil_tanks",
        ["Meters"] = "meters",
        ["Lookups"] = "lookups",
        ["StationReadings"] = "station_readings",
        ["SubStationReadings"] = "station_readings",
        ["FuelMovements"] = "fuel_movements",
        ["OilTransactions"] = "oil_transactions",
        ["Inventory"] = "periodic_inventory",
        ["Reports"] = "reports",
        ["Audits"] = "audit_logs",
        ["Notifications"] = "notifications",
        ["Dashboard"] = "dashboard",
        ["Screens"] = "controllers",
        ["ControllersDevice"] = "controllers",
        ["Efficiency"] = "generators",
        ["Rollback"] = "rollback_approval",
        ["InventoryDashboard"] = "periodic_inventory",
        ["ReportsDashboard"] = "reports",
        ["StationDashboard"] = "stations",
        ["SubStationDashboard"] = "sub_stations",
        ["GeneratorDashboard"] = "generators",
        ["OilDashboard"] = "oil_tanks",
    };

    private static readonly Dictionary<string, string> MethodToAction = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GET"] = "view",
        ["POST"] = "create",
        ["PUT"] = "update",
        ["PATCH"] = "update",
        ["DELETE"] = "delete",
    };

    /// <summary>
    /// Resolves the permission key for a given controller and HTTP method.
    /// Returns null if no mapping exists (endpoint doesn't require permission).
    /// </summary>
    public static string? ResolvePermissionKey(string controllerName, string httpMethod)
    {
        if (!ControllerToModule.TryGetValue(controllerName, out var moduleKey))
            return null;

        if (!MethodToAction.TryGetValue(httpMethod, out var actionKey))
            return null;

        return $"{moduleKey}.{actionKey}";
    }
}

/// <summary>
/// Middleware that enforces page-level permissions on every API request.
/// Uses in-memory cache (via PermissionCache) to avoid DB queries on every request.
/// Uses existing request scope instead of creating new ones.
/// </summary>
public class PermissionEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PermissionEnforcementMiddleware> _logger;

    private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/health",
        "/api/health",
    };

    public PermissionEnforcementMiddleware(RequestDelegate next, ILogger<PermissionEnforcementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        if (ExemptPaths.Contains(path) || !path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // Operator self-service endpoints are inherently scoped to the caller
        // (their own notifications / their own stations & meters / creating a
        // station reading). Allow any authenticated user so the Station Reading
        // flow works without granting broad module permissions.
        if (IsAllowedForAuthenticatedUser(path, context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (IsSystemOwner(context.User!))
        {
            await _next(context);
            return;
        }

        var controllerName = GetControllerName(context);
        if (string.IsNullOrEmpty(controllerName))
        {
            await _next(context);
            return;
        }

        var permissionKey = PermissionMapping.ResolvePermissionKey(controllerName, context.Request.Method);
        if (permissionKey == null)
        {
            await _next(context);
            return;
        }

        var hasPermission = await CheckPermissionAsync(context, permissionKey);
        if (!hasPermission)
        {
            _logger.LogWarning(
                "Permission denied: User {UserId} lacks '{Permission}' for {Method} {Path}",
                GetUserId(context.User!), permissionKey, context.Request.Method, path);

            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = $"You do not have permission to perform this action. Required: {permissionKey}"
            });
            return;
        }

        await _next(context);
    }

    private static bool IsSystemOwner(ClaimsPrincipal user)
    {
        return user.IsInRole("SystemOwner") ||
               user.IsInRole("Admin") ||
               user.HasClaim(c => c.Type == "role" && (c.Value == "SystemOwner" || c.Value == "Admin"));
    }

    /// <summary>
    /// Self-service endpoints that are inherently scoped to the caller
    /// (their own notifications, their own generators, and creating a
    /// station reading). Any authenticated user is allowed so these flows
    /// work without granting broad module permissions.
    /// </summary>
    private static bool IsAllowedForAuthenticatedUser(string path, string method)
    {
        if (path.StartsWith("/api/notifications", StringComparison.OrdinalIgnoreCase))
            return true;

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
            path.Equals("/api/substation-readings", StringComparison.OrdinalIgnoreCase))
            return true;

        if (path.StartsWith("/api/generators/my", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static int GetUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : 0;
    }

    private static string? GetControllerName(HttpContext context)
    {
        if (context.Request.RouteValues.TryGetValue("controller", out var controller))
            return controller?.ToString();

        var segments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments != null && segments.Length >= 2 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
            return segments[1];

        return null;
    }

    private static async Task<bool> CheckPermissionAsync(HttpContext context, string permissionKey)
    {
        var dotIndex = permissionKey.IndexOf('.');
        var moduleKey = dotIndex > 0 ? permissionKey[..dotIndex] : permissionKey;
        var actionKey = dotIndex > 0 ? permissionKey[(dotIndex + 1)..] : string.Empty;

        var userId = GetUserId(context.User!);
        var roleNames = context.User!.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        if (roleNames.Count == 0)
            return false;

        if (PermissionCache.TryGet(userId, moduleKey, actionKey, out var cached))
            return cached;

        var db = context.RequestServices.GetRequiredService<FuelTrackDbContext>();

        var hasPermission = await db.RolePermissions
            .AsNoTracking()
            .AnyAsync(rp =>
                rp.Role.Name != null && roleNames.Contains(rp.Role.Name) &&
                rp.IsGranted &&
                rp.PagePermission.ModuleDefinition.Key == moduleKey &&
                rp.PagePermission.ActionKey == actionKey &&
                rp.PagePermission.IsActive &&
                rp.PagePermission.ModuleDefinition.IsActive);

        PermissionCache.Set(userId, moduleKey, actionKey, hasPermission);

        return hasPermission;
    }
}
