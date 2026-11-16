using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Tests.Services;

public sealed class CommercialStationModeServiceTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly CommercialStationModeService _service;

    public CommercialStationModeServiceTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"CommercialStationMode_{Guid.NewGuid()}").Options);
        _service = new CommercialStationModeService(_db);
    }

    [Fact]
    public async Task ActivateAsync_EmptyTenant_ProvisionsOneHiddenTechnicalHierarchy()
    {
        var result = await _service.ActivateAsync(new ActivateCommercialStationModeDto("Commercial Station", "CS-01"), 7);

        Assert.True(result.IsSuccess);
        Assert.True(await _service.IsCommercialModeAsync());
        Assert.Single(await _db.Branches.Where(x => x.DeletedAt == null && x.IsActive).ToListAsync());
        Assert.Single(await _db.Stations.Where(x => x.DeletedAt == null && x.IsActive).ToListAsync());
        var site = Assert.Single(await _db.SubStations.Where(x => x.DeletedAt == null && x.IsActive).ToListAsync());
        var station = Assert.Single(await _db.Stations.ToListAsync());
        Assert.Equal(station.Code, site.Code);
        Assert.Equal(station.Id, site.StationId);
    }

    [Fact]
    public async Task ActivationPreflight_MultipleActiveHierarchyRows_BlocksWithoutMutation()
    {
        await SeedHierarchyAsync("A", "A");
        await SeedHierarchyAsync("B", "B");

        var preflight = await _service.GetActivationPreflightAsync();
        var result = await _service.ActivateAsync(new ActivateCommercialStationModeDto("Commercial Station", "CS-01"), 7);

        Assert.False(preflight.CanActivate);
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.False(await _service.IsCommercialModeAsync());
    }

    [Fact]
    public async Task CommercialScope_IgnoresUserScopeAssignmentsAndResolvesOnlyTechnicalSite()
    {
        await _service.ActivateAsync(new ActivateCommercialStationModeDto("Commercial Station", "CS-01"), 7);
        var technicalSite = Assert.Single(await _db.SubStations.ToListAsync());
        _db.UserScopes.Add(new UserScope { UserId = 42, BranchId = 999, StationId = 998, SubStationId = 997 });
        await _db.SaveChangesAsync();
        var scopeFilter = new UserScopeFilterService(_db, _service);

        var accessibleSites = await scopeFilter.GetAccessibleSubStationIdsAsync(42);
        var accessibleStations = await scopeFilter.GetAccessibleStationIdsAsync(42);

        Assert.Equal([technicalSite.Id], accessibleSites);
        Assert.Equal([technicalSite.StationId], accessibleStations);
        Assert.True(await scopeFilter.HasAccessToSubStationAsync(42, technicalSite.Id));
        Assert.False(await scopeFilter.HasAccessToSubStationAsync(42, 997));
    }

    [Fact]
    public async Task EnterpriseMode_PreservesExistingUserScopeResolution()
    {
        var site = await SeedHierarchyAsync("Enterprise", "ENT");
        _db.UserScopes.Add(new UserScope { UserId = 19, BranchId = null, StationId = null, SubStationId = site.Id });
        await _db.SaveChangesAsync();
        var scopeFilter = new UserScopeFilterService(_db, _service);

        Assert.False(await _service.IsCommercialModeAsync());
        Assert.Equal([site.Id], await scopeFilter.GetAccessibleSubStationIdsAsync(19));
    }

    [Fact]
    public async Task SeparateTenantDatabases_DoNotShareCommercialTechnicalScope()
    {
        await _service.ActivateAsync(new ActivateCommercialStationModeDto("Tenant One", "ONE"), 1);
        var firstTenantSite = Assert.Single(await _db.SubStations.ToListAsync());

        await using var otherDb = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"CommercialStationModeOther_{Guid.NewGuid()}").Options);
        var otherService = new CommercialStationModeService(otherDb);
        await otherService.ActivateAsync(new ActivateCommercialStationModeDto("Tenant Two", "TWO"), 2);
        var otherTenantSite = Assert.Single(await otherDb.SubStations.ToListAsync());
        var otherScopeFilter = new UserScopeFilterService(otherDb, otherService);

        Assert.NotEqual(firstTenantSite.Code, otherTenantSite.Code);
        Assert.False(await otherDb.SubStations.AnyAsync(site => site.Code == firstTenantSite.Code));
        Assert.True(await otherScopeFilter.HasAccessToSubStationAsync(1, otherTenantSite.Id));
    }

    public void Dispose() => _db.Dispose();

    private async Task<SubStation> SeedHierarchyAsync(string name, string code)
    {
        var branch = new Branch { Name = $"Branch {name}", Code = $"B-{code}" };
        var station = new Station { Branch = branch, Name = $"Station {name}", Code = $"S-{code}" };
        var site = new SubStation { Station = station, Name = $"Site {name}", Code = $"SS-{code}" };
        _db.Add(site);
        await _db.SaveChangesAsync();
        return site;
    }
}
