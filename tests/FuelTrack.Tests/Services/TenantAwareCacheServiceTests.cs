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
    public async Task TenantMode_DoesNotCacheOrServeStaleValuesAcrossReplicas()
    {
        using var memory = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        using var otherMemory = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        var distributed = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var shared = new HybridCacheService(memory, distributed, NullLogger<HybridCacheService>.Instance);
        var otherInstance = new HybridCacheService(otherMemory, distributed, NullLogger<HybridCacheService>.Instance);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["MultiTenancy:Enabled"] = "true" }).Build();
        var firstTenant = new Tenant(Guid.NewGuid());
        var first = new TenantAwareCacheService(shared, config, firstTenant);
        var firstOnAnotherInstance = new TenantAwareCacheService(otherInstance, config, firstTenant);
        var second = new TenantAwareCacheService(shared, config, new Tenant(Guid.NewGuid()));
        await shared.SetAsync($"tenant:{firstTenant.Id:N}:dashboard:stats:admin", "stale from old instance");
        await first.SetAsync("dashboard:stats:admin", "first");
        await second.SetAsync("dashboard:stats:admin", "second");
        Assert.Null(await first.GetAsync<string>("dashboard:stats:admin"));
        Assert.Null(await firstOnAnotherInstance.GetAsync<string>("dashboard:stats:admin"));
        Assert.Null(await second.GetAsync<string>("dashboard:stats:admin"));
        await first.RemoveByPrefixAsync("dashboard:");
        Assert.Null(await first.GetAsync<string>("dashboard:stats:admin"));
        Assert.Null(await firstOnAnotherInstance.GetAsync<string>("dashboard:stats:admin"));
        Assert.Null(await second.GetAsync<string>("dashboard:stats:admin"));
    }

    [Fact]
    public async Task MissingTenantInMultiTenantMode_FailsClosed()
    {
        using var memory = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        var inner = new HybridCacheService(memory, new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())), NullLogger<HybridCacheService>.Instance);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["MultiTenancy:Enabled"] = "true" }).Build();
        var service = new TenantAwareCacheService(inner, config);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAsync<string>("any"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetAsync("any", "value"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemoveAsync("any"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemoveByPrefixAsync("any"));
    }

    [Fact]
    public async Task LegacyMode_PreservesCacheAndPrefixInvalidation()
    {
        using var memory = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        var distributed = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var inner = new HybridCacheService(memory, distributed, NullLogger<HybridCacheService>.Instance);
        var service = new TenantAwareCacheService(inner, new ConfigurationBuilder().Build());
        await service.SetAsync("dashboard:one", "legacy");
        Assert.Equal("legacy", await service.GetAsync<string>("dashboard:one"));
        await service.RemoveByPrefixAsync("dashboard:");
        Assert.Null(await service.GetAsync<string>("dashboard:one"));
    }

    private sealed record Tenant(Guid Id) : ITenantRequestContext
    {
        public Guid? TenantId => Id;
        public string? DatabaseConnection => null;
    }
}
