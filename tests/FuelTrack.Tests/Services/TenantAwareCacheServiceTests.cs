using FuelTrack.Application.Interfaces;
using FuelTrack.Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FuelTrack.Tests.Services;

public sealed class TenantAwareCacheServiceTests
{
    [Fact]
    public async Task IdenticalKeysAndPrefixInvalidation_AreIsolatedBetweenTenants()
    {
        using var memory = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        var distributed = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var shared = new HybridCacheService(memory, distributed, NullLogger<HybridCacheService>.Instance);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["MultiTenancy:Enabled"] = "true" }).Build();
        var first = new TenantAwareCacheService(shared, config, new Tenant(Guid.NewGuid()));
        var second = new TenantAwareCacheService(shared, config, new Tenant(Guid.NewGuid()));
        await first.SetAsync("dashboard:stats:admin", "first");
        await second.SetAsync("dashboard:stats:admin", "second");
        Assert.Equal("first", await first.GetAsync<string>("dashboard:stats:admin"));
        Assert.Equal("second", await second.GetAsync<string>("dashboard:stats:admin"));
        await first.RemoveByPrefixAsync("dashboard:");
        Assert.Null(await first.GetAsync<string>("dashboard:stats:admin"));
        Assert.Equal("second", await second.GetAsync<string>("dashboard:stats:admin"));
    }

    [Fact]
    public async Task MissingTenantInMultiTenantMode_FailsClosed()
    {
        using var memory = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        var inner = new HybridCacheService(memory, new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())), NullLogger<HybridCacheService>.Instance);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["MultiTenancy:Enabled"] = "true" }).Build();
        var service = new TenantAwareCacheService(inner, config);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAsync<string>("any"));
    }

    private sealed record Tenant(Guid Id) : ITenantRequestContext
    {
        public Guid? TenantId => Id;
        public string? DatabaseConnection => null;
    }
}
