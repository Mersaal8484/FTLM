using FuelTrack.Application.Common;
using FuelTrack.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FuelTrack.Infrastructure.Services;

/// <summary>Scopes every cache operation, including prefix invalidation, to the trusted tenant.</summary>
public sealed class TenantAwareCacheService(HybridCacheService inner, IConfiguration configuration, ITenantRequestContext? tenant = null) : ICacheService
{
    private string Key(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!configuration.GetValue<bool>("MultiTenancy:Enabled")) return key;
        if (tenant?.TenantId is not Guid id || id == Guid.Empty)
            throw new InvalidOperationException("A trusted tenant context is required for cache access.");
        return $"tenant:{id:N}:{key}";
    }
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) => inner.GetAsync<T>(Key(key), ct);
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) => inner.SetAsync(Key(key), value, expiry, ct);
    public Task RemoveAsync(string key, CancellationToken ct = default) => inner.RemoveAsync(Key(key), ct);
    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default) => inner.RemoveByPrefixAsync(Key(prefix), ct);
}
