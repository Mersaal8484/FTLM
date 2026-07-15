using System.Collections.Concurrent;

namespace FuelTrack.Application.Common;

/// <summary>
/// Static in-memory cache for permission checks.
/// Keyed by "userId:moduleKey:actionKey".
/// Call ClearAll() or ClearForUser(userId) after role/permission changes.
/// </summary>
public static class PermissionCache
{
    private static readonly ConcurrentDictionary<string, (bool HasPermission, DateTime ExpiresAt)> _cache = new();
    private static readonly TimeSpan Duration = TimeSpan.FromMinutes(5);

    public static bool TryGet(int userId, string moduleKey, string actionKey, out bool hasPermission)
    {
        var key = $"{userId}:{moduleKey}:{actionKey}";
        if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            hasPermission = entry.HasPermission;
            return true;
        }
        hasPermission = false;
        return false;
    }

    public static void Set(int userId, string moduleKey, string actionKey, bool hasPermission)
    {
        var key = $"{userId}:{moduleKey}:{actionKey}";
        _cache[key] = (hasPermission, DateTime.UtcNow.Add(Duration));
    }

    public static void ClearForUser(int userId)
    {
        var prefix = $"{userId}:";
        var keys = _cache.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var key in keys)
            _cache.TryRemove(key, out _);
    }

    public static void ClearAll()
    {
        _cache.Clear();
    }
}
