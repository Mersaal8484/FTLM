using System.Security.Claims;
using FuelTrack.Application.Common;
using FuelTrack.Application.Interfaces;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
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
        ["Attachments"] = "operational_data",
        ["StationOperationalSettings"] = "operational_data",
        ["OperationalCaptures"] = "operational_data",
        ["TankCalibrations"] = "operational_data",
        ["OperationalFeeders"] = "operational_data",
        ["CustomerFeatures"] = "settings",
        ["FuelMovements"] = "fuel_movements",
        ["MovementLedger"] = "fuel_movements",
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
        ["SpareParts"] = "warehouse",
        ["WarehouseStock"] = "warehouse",
        ["WarehouseSupply"] = "warehouse",
        ["WarehouseTransactions"] = "warehouse",
        ["WarehouseReports"] = "warehouse",
        ["DispatchRequest"] = "warehouse",
        ["WorkPermits"] = "maintenance",
        ["InstalledParts"] = "maintenance",
        ["MaintenanceDashboard"] = "maintenance",
        ["ServiceItems"] = "maintenance",
        ["ServiceIntervalDefinitions"] = "maintenance",
        ["ServiceReplacements"] = "maintenance",
        ["ServiceDue"] = "maintenance",
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
    /// Sub-path actions that represent a privileged approval-style operation and
    /// are therefore authorized by the module's "approve" permission (granted to
    /// the user's role) rather than the generic create/update permission.
    /// </summary>
    private static readonly HashSet<string> PrivilegedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "approve",
        "reject",
        "execute",
        "review",
    };

    /// <summary>
    /// Resolves the permission key for a given controller and HTTP method.
    /// Returns null if no mapping exists (endpoint doesn't require permission).
    /// </summary>
    public static string? ResolvePermissionKey(string controllerName, string httpMethod, string? path = null)
    {
        if (!ControllerToModule.TryGetValue(controllerName, out var moduleKey))
            return null;

        if (!MethodToAction.TryGetValue(httpMethod, out var actionKey))
            return null;

        // Phase-one foundation endpoints are intentionally mapped to the
        // granular operational permissions rather than generic CRUD verbs.
        if (controllerName.Equals("Attachments", StringComparison.OrdinalIgnoreCase) &&
            httpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            actionKey = "capture";
        else if (controllerName.Equals("StationOperationalSettings", StringComparison.OrdinalIgnoreCase) &&
                 !httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            actionKey = "configure";
        else if (controllerName.Equals("OperationalCaptures", StringComparison.OrdinalIgnoreCase) &&
                 httpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            actionKey = "capture";
        else if (controllerName.Equals("TankCalibrations", StringComparison.OrdinalIgnoreCase))
            actionKey = "configure";
        else if (controllerName.Equals("OperationalFeeders", StringComparison.OrdinalIgnoreCase) && httpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            actionKey = "configure";
        else if (controllerName is "ServiceItems" or "ServiceIntervalDefinitions")
            actionKey = httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) ? "view" : "configure_service";
        else if (controllerName.Equals("ServiceReplacements", StringComparison.OrdinalIgnoreCase))
            actionKey = httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) ? "view" : "record_service";
        else if (controllerName.Equals("ServiceDue", StringComparison.OrdinalIgnoreCase))
            actionKey = httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) ? "view" : "record_service";

        // Privileged sub-actions (e.g. POST /api/fuel-movements/1/approve,
        // POST /api/rollback/execute, POST /api/readings/1/review) are authorized
        // by a dedicated "approve" permission granted to the role, not by the
        // generic create permission.
        if (path != null)
        {
            var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var last = segments.Length > 0 ? segments[^1] : string.Empty;
            if (PrivilegedActions.Contains(last))
            {
                actionKey = "approve";
            }
        }

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

        var controllerName = GetControllerName(context);
        if (string.IsNullOrEmpty(controllerName))
        {
            await _next(context);
            return;
        }

        var isNewServiceController = controllerName is "ServiceItems"
            or "ServiceIntervalDefinitions"
            or "ServiceReplacements"
            or "ServiceDue";

        // For the new Phase 4 Service controllers only: stricter check via IsOperationalDataNewModeAsync.
        // System owners do not bypass customer feature gates.
        if (isNewServiceController)
        {
            if (!await IsServiceIntervalFeatureEnabledAsync(context))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
        }
        else
        {
            // For all existing controllers (Phases 1-3b), restore original behavior:
            // SystemOwner bypasses individual action permission assignments immediately.
            if (IsSystemOwner(context.User!))
            {
                await _next(context);
                return;
            }
        }

        var permissionKey = PermissionMapping.ResolvePermissionKey(controllerName, context.Request.Method, path);
        if (permissionKey == null)
        {
            await _next(context);
            return;
        }

        var moduleKey = permissionKey[..permissionKey.IndexOf('.')];

        // Check feature gate for existing operational_data controllers
        if (!isNewServiceController && !await IsFeatureEnabledAsync(context, moduleKey))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // For new service controllers, SystemOwner bypasses permission check after feature gate passed
        if (isNewServiceController && IsSystemOwner(context.User!))
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

    private static async Task<bool> IsFeatureEnabledAsync(HttpContext context, string moduleKey)
    {
        // Legacy warehouse and maintenance endpoints retain their existing
        // behavior. Only the new operational surface is feature-gated here.
        if (!moduleKey.Equals("operational_data", StringComparison.OrdinalIgnoreCase)) return true;

        var db = context.RequestServices.GetRequiredService<FuelTrackDbContext>();
        return await db.CustomerFeatureSettings
            .AsNoTracking()
            .AnyAsync(x => x.Key == moduleKey && x.IsEnabled);
    }

    private static async Task<bool> IsServiceIntervalFeatureEnabledAsync(HttpContext context)
    {
        var featureService = context.RequestServices.GetService<ICustomerFeatureService>()
            ?? new CustomerFeatureService(context.RequestServices.GetRequiredService<FuelTrackDbContext>());
        return await featureService.IsOperationalDataNewModeAsync();
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

        // Supporting read endpoints for the Site Energy Monitoring (station reading)
        // entry flow and generator reading entry flow. An operator creating a
        // station/generator reading needs the linked site meter or controller,
        // but should not require broad sub_stations/meters/controllers.view.
        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            if (path.Equals("/api/customer-features/operational-data-mode", StringComparison.OrdinalIgnoreCase))
                return true;
            if (path.StartsWith("/api/sub-stations", StringComparison.OrdinalIgnoreCase))
                return true;
            if (path.StartsWith("/api/meters", StringComparison.OrdinalIgnoreCase))
                return true;
            if (path.StartsWith("/api/controllers", StringComparison.OrdinalIgnoreCase))
                return true;
        }

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

        var cacheKey = $"perm:{userId}:{moduleKey}:{actionKey}";
        var cacheService = context.RequestServices.GetService<ICacheService>();

        if (cacheService != null)
        {
            var cached = await cacheService.GetAsync<bool?>(cacheKey);
            if (cached.HasValue) return cached.Value;
        }

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

        if (cacheService != null)
        {
            await cacheService.SetAsync(cacheKey, hasPermission, TimeSpan.FromMinutes(5));
        }
        else
        {
            PermissionCache.Set(userId, moduleKey, actionKey, hasPermission);
        }

        return hasPermission;
    }
}
