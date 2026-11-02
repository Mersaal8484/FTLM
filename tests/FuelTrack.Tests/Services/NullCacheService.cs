using FuelTrack.Application.Common;

namespace FuelTrack.Tests.Services;

public class NullCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        => Task.FromResult<T?>(default);
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
        => Task.CompletedTask;
    public Task RemoveAsync(string key, CancellationToken ct = default)
        => Task.CompletedTask;
    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
        => Task.CompletedTask;
}
