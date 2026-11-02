using System.Security.Claims;
using System.Text.Json;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    private static readonly HashSet<string> TrackedEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        "FuelTransactions", "OilTransactions", "DailyReadings", "SubStationReadings",
        "PeriodicInventories", "Generators", "FuelTanks", "OilTanks", "Meters",
        "SubStations", "Stations", "Users", "Roles", "Lookups", "AnalyticsInsights",
        "Controllers", "Notifications", "Oils"
    };

    private static readonly Dictionary<string, string> RouteEntityMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fuel-movements"] = "FuelTransactions",
        ["oil-transactions"] = "OilTransactions",
        ["readings"] = "DailyReadings",
        ["substation-readings"] = "SubStationReadings",
        ["inventory"] = "PeriodicInventories",
        ["generators"] = "Generators",
        ["fuel-tanks"] = "FuelTanks",
        ["oils"] = "Oils",
        ["meters"] = "Meters",
        ["sub-stations"] = "SubStations",
        ["stations"] = "Stations",
        ["users"] = "Users",
        ["roles"] = "Roles",
        ["lookups"] = "Lookups",
        ["analytics"] = "AnalyticsInsights",
        ["controllers"] = "Controllers",
        ["notifications"] = "Notifications"
    };

    private static readonly Dictionary<string, Type> EntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Users"] = typeof(User),
        ["Roles"] = typeof(Role),
        ["Stations"] = typeof(Station),
        ["SubStations"] = typeof(SubStation),
        ["Generators"] = typeof(Generator),
        ["FuelTanks"] = typeof(FuelTank),
        ["Meters"] = typeof(Meter),
        ["Lookups"] = typeof(Lookup),
        ["DailyReadings"] = typeof(DailyReading),
        ["FuelTransactions"] = typeof(FuelTransaction),
        ["OilTransactions"] = typeof(OilTransaction),
        ["PeriodicInventories"] = typeof(PeriodicInventory),
        ["SubStationReadings"] = typeof(SubStationReading),
        ["AnalyticsInsights"] = typeof(AnalyticsInsight),
        ["Controllers"] = typeof(Controller),
        ["Notifications"] = typeof(Notification),
        ["Oils"] = typeof(Oil)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public AuditLoggingMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        if (method is not ("POST" or "PUT" or "DELETE" or "PATCH"))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";
        var entityName = ExtractEntityName(path);
        if (entityName is null || !TrackedEntities.Contains(entityName))
        {
            await _next(context);
            return;
        }

        var userId = GetUserId(context);
        var action = method switch
        {
            "POST" => "Create",
            "PUT" or "PATCH" => "Update",
            "DELETE" => "Delete",
            _ => method
        };

        var entityId = ExtractEntityIdFromPath(path);
        string? bodyBefore = null;
        string? bodyAfter = null;

        if (method is "PUT" or "PATCH")
        {
            bodyBefore = await ReadExistingEntityAsync(entityName, path);
        }

        var originalResponseBody = context.Response.Body;
        var responseBodyBuffer = new MemoryStream();
        context.Response.Body = responseBodyBuffer;

        try
        {
            await _next(context);

            responseBodyBuffer.Position = 0;
            if (method is "PUT" or "PATCH")
            {
                bodyAfter = await new StreamReader(responseBodyBuffer).ReadToEndAsync();
            }
            else if (method is "DELETE")
            {
                bodyBefore = await ReadExistingEntityAsync(entityName, path);
            }

            responseBodyBuffer.Position = 0;
            if (responseBodyBuffer.Length > 0)
            {
                await responseBodyBuffer.CopyToAsync(originalResponseBody);
            }
            context.Response.Body = originalResponseBody;

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                _ = SaveAuditLogAsync(userId, action, entityName, entityId, bodyBefore, bodyAfter, context.Connection.RemoteIpAddress?.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUDIT] Error processing {Action} on {Entity} at {Path}", action, entityName, path);
            context.Response.Body = originalResponseBody;
            throw;
        }
        finally
        {
            await responseBodyBuffer.DisposeAsync();
        }
    }

    private static string? ExtractEntityName(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i].Equals("api", StringComparison.OrdinalIgnoreCase) && i + 1 < segments.Length)
            {
                var segment = segments[i + 1];
                if (RouteEntityMap.TryGetValue(segment, out var entityName))
                    return entityName;

                if (i + 2 < segments.Length)
                {
                    var compound = segment + "/" + segments[i + 2];
                    if (RouteEntityMap.TryGetValue(compound, out var compoundEntityName))
                        return compoundEntityName;
                }
                return null;
            }
        }
        return null;
    }

    private static int ExtractEntityIdFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = segments.Length - 1; i >= 0; i--)
        {
            if (int.TryParse(segments[i], out var id))
                return id;
        }
        return 0;
    }

    private static int GetUserId(HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null ? int.Parse(claim.Value) : 0;
    }

    private async Task<string?> ReadExistingEntityAsync(string entityName, string path)
    {
        try
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var idStr = segments.LastOrDefault();
            if (idStr is null || !int.TryParse(idStr, out var entityId))
                return null;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();

            if (!EntityTypes.TryGetValue(entityName, out var entityType))
                return null;

            var entity = await db.FindAsync(entityType, entityId);
            return entity is not null ? JsonSerializer.Serialize(entity, JsonOptions) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AUDIT] Failed to read existing entity {Entity} at {Path}", entityName, path);
            return null;
        }
    }

    private async Task SaveAuditLogAsync(int userId, string action, string entityName, int entityId, string? oldValues, string? newValues, string? ip)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();

            db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValues = oldValues?.Length > 4000 ? oldValues[..4000] : oldValues,
                NewValues = newValues?.Length > 4000 ? newValues[..4000] : newValues,
                IpAddress = ip,
                Timestamp = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUDIT] Failed to save audit log for {Action} on {Entity}", action, entityName);
        }
    }
}
