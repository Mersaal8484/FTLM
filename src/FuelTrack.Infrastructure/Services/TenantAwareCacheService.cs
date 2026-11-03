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
    // The legacy hybrid cache keeps a process-local key index and memory layer.
    // Prefix invalidation is therefore not coherent across replicas. In tenant
    // mode bypass it until a shared cache with distributed invalidation exists.
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var scopedKey = Key(key);
        return configuration.GetValue<bool>("MultiTenancy:Enabled") ? Task.FromResult<T?>(default) : inner.GetAsync<T>(scopedKey, ct);
    }
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var scopedKey = Key(key);
        return configuration.GetValue<bool>("MultiTenancy:Enabled") ? Task.CompletedTask : inner.SetAsync(scopedKey, value, expiry, ct);
    }
    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        var scopedKey = Key(key);
        return configuration.GetValue<bool>("MultiTenancy:Enabled") ? Task.CompletedTask : inner.RemoveAsync(scopedKey, ct);
    }
    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var scopedKey = Key(prefix);
        return configuration.GetValue<bool>("MultiTenancy:Enabled") ? Task.CompletedTask : inner.RemoveByPrefixAsync(scopedKey, ct);
    }
}
