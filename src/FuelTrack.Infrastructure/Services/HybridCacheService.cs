using System.Text.Json;
using FuelTrack.Application.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FuelTrack.Infrastructure.Services;

public class HybridCacheService : ICacheService
{
    private readonly IMemoryCache _memory;
    private readonly IDistributedCache _distributed;
    private readonly ILogger<HybridCacheService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public HybridCacheService(
        IMemoryCache memory,
        IDistributedCache distributed,
        ILogger<HybridCacheService> logger)
    {
        _memory = memory;
        _distributed = distributed;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        if (_memory.TryGetValue(key, out T? memValue) && memValue is not null)
        {
            _logger.LogDebug("Cache HIT (memory): {Key}", key);
            return memValue;
        }

        try
        {
            var bytes = await _distributed.GetAsync(key, ct);
            if (bytes is not null)
            {
                var value = JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
                if (value is not null)
                {
                    _logger.LogDebug("Cache HIT (distributed): {Key}", key);
                    _memory.Set(key, value, TimeSpan.FromMinutes(2));
                    return value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Distributed cache read failed for {Key}", key);
        }

        _logger.LogDebug("Cache MISS: {Key}", key);
        return null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        var ttl = expiry ?? TimeSpan.FromMinutes(5);

        _memory.Set(key, value, ttl);

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
            await _distributed.SetAsync(key, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Distributed cache write failed for {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _memory.Remove(key);

        try
        {
            await _distributed.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Distributed cache remove failed for {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        _logger.LogInformation("Cache purge prefix: {Prefix}", prefix);

        if (_memory is MemoryCache memCache)
        {
            var field = typeof(MemoryCache).GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(memCache) is System.Collections.IDictionary entries)
            {
                var keysToRemove = new List<object>();
                foreach (System.Collections.DictionaryEntry entry in entries)
                {
                    if (entry.Key.ToString()?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true)
                        keysToRemove.Add(entry.Key);
                }
                foreach (var k in keysToRemove)
                    entries.Remove(k);
            }
        }

        await Task.CompletedTask;
    }
}
