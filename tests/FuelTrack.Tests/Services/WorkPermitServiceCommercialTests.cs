using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FuelTrack.Tests.Services;

public sealed class WorkPermitServiceCommercialTests : IDisposable
{
    private readonly FuelTrackDbContext _db;

    public WorkPermitServiceCommercialTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"WorkPermitServiceCommercial_{Guid.NewGuid()}").Options);

        var maintTypeCat = new LookupCategory { Id = 1, Name = "MaintenanceType", DisplayName = "Maintenance Type", IsActive = true };
        var deptCat = new LookupCategory { Id = 2, Name = "MaintenanceDepartment", DisplayName = "Department", IsActive = true };
        var priorityCat = new LookupCategory { Id = 3, Name = "MaintenancePriority", DisplayName = "Priority", IsActive = true };
        var statusCat = new LookupCategory { Id = 4, Name = "MaintenanceStatus", DisplayName = "Status", IsActive = true };

        _db.AddRange(
            maintTypeCat, deptCat, priorityCat, statusCat,
            new Lookup { Id = 10, CategoryId = 1, Category = maintTypeCat, Code = "Corrective", Name = "تصحيحية", IsActive = true },
            new Lookup { Id = 20, CategoryId = 2, Category = deptCat, Code = "Electrical", Name = "كهرباء", IsActive = true },
            new Lookup { Id = 30, CategoryId = 3, Category = priorityCat, Code = "Normal", Name = "عادية", IsActive = true },
            new Lookup { Id = 40, CategoryId = 4, Category = statusCat, Code = "Draft", Name = "مسودة", IsActive = true },
            new Branch { Id = 1, Name = "Technical Branch", Code = "TECH-PS01", IsActive = true },
            new Station { Id = 1, BranchId = 1, Name = "Commercial Station", Code = "PS01", IsActive = true },
            // Id=1: the single commercial scope (resolver requires exactly 1 active SubStation via DeletedAt==null && IsActive)
            new SubStation { Id = 1, StationId = 1, Name = "Commercial Station", Code = "PS01", IsActive = true },
            // Id=2: seeded INACTIVE so the resolver sees Count==1 in commercial-mode tests.
            // Enterprise tests that need SubStation 2 must activate it locally before the act.
            new SubStation { Id = 2, StationId = 1, Name = "Other Site", Code = "OTH", IsActive = false },
            new Generator { Id = 101, SubStationId = 1, Name = "Gen 1", SerialNumber = "GEN01", IsActive = true },
            new Generator { Id = 102, SubStationId = 2, Name = "Gen Other Site", SerialNumber = "GEN02", IsActive = true },
            new User { Id = 1, UserName = "admin@ft.com", Email = "admin@ft.com", FullName = "Admin User", IsActive = true }
        );
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void EnableCommercialMode()
    {
        var existing = _db.CustomerFeatureSettings.FirstOrDefault(f => f.Key == CommercialStationModeService.FeatureKey);
        if (existing != null)
        {
            existing.IsEnabled = true;
            existing.Value = CommercialStationModeService.CommercialModeValue;
        }
        else
        {
            _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
            {
                Key = CommercialStationModeService.FeatureKey,
                IsEnabled = true,
                Value = CommercialStationModeService.CommercialModeValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_CommercialMode_ResolvesTechnicalScopeAndStoresSubStationId()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var dto = new CreateWorkPermitDto(
            SubStationId: 0, // Client passes 0 or omits
            GeneratorId: 101,
            MaintenanceTypeLookupId: 10,
            DepartmentLookupId: 20,
            PriorityLookupId: 30,
            PermitDate: DateTime.UtcNow,
            ProblemDescription: "Routine test");

        var result = await service.CreateAsync(dto, 1);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.SubStationId);
        Assert.StartsWith("WP-", result.Value.PermitNumber);
    }

    [Fact]
    public async Task CreateAsync_CommercialMode_WithNoSite_Succeeds()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var dto = new CreateWorkPermitDto(
            SubStationId: 0,
            GeneratorId: null,
            MaintenanceTypeLookupId: 10,
            DepartmentLookupId: 20,
            PriorityLookupId: 30,
            PermitDate: DateTime.UtcNow,
            ProblemDescription: "General station maintenance");

        var result = await service.CreateAsync(dto, 1);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.SubStationId);
    }

    [Fact]
    public async Task CreateAsync_CommercialMode_UnresolvableScope_ReturnsCleanFailure()
    {
        EnableCommercialMode();
        // Make all SubStations inactive/deleted so the resolver returns null (Count != 1)
        // Using soft-delete instead of RemoveRange to avoid FK violations from Generator references
        var subs = _db.SubStations.ToList();
        foreach (var s in subs) { s.IsActive = false; s.DeletedAt = DateTime.UtcNow; }
        _db.SaveChanges();

        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var dto = new CreateWorkPermitDto(
            SubStationId: 0,
            MaintenanceTypeLookupId: 10,
            DepartmentLookupId: 20,
            PriorityLookupId: 30,
            PermitDate: DateTime.UtcNow,
            ProblemDescription: "Scope test");

        var result = await service.CreateAsync(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Contains("Commercial mode has no active technical station scope", result.Error);
    }

    [Fact]
    public async Task CreateAsync_CommercialMode_GeneratorFromOtherSite_ReturnsCleanFailure()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var dto = new CreateWorkPermitDto(
            SubStationId: 0,
            GeneratorId: 102, // Belongs to SubStation 2, not commercial station (1)
            MaintenanceTypeLookupId: 10,
            DepartmentLookupId: 20,
            PriorityLookupId: 30,
            PermitDate: DateTime.UtcNow,
            ProblemDescription: "Generator mismatch");

        var result = await service.CreateAsync(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal("Generator does not belong to the permit sub-station.", result.Error);
    }

    [Fact]
    public async Task CreateAsync_EnterpriseMode_RequiresSubStationId_FailsWhenZero()
    {
        // Enterprise mode (commercial mode not enabled)
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var dto = new CreateWorkPermitDto(
            SubStationId: 0,
            MaintenanceTypeLookupId: 10,
            DepartmentLookupId: 20,
            PriorityLookupId: 30,
            PermitDate: DateTime.UtcNow,
            ProblemDescription: "Enterprise test");

        var result = await service.CreateAsync(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal("SubStationId is required outside commercial mode.", result.Error);
    }

    [Fact]
    public async Task CreateAsync_EnterpriseMode_InScopeSite_Succeeds()
    {
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var dto = new CreateWorkPermitDto(
            SubStationId: 2,
            GeneratorId: 102,
            MaintenanceTypeLookupId: 10,
            DepartmentLookupId: 20,
            PriorityLookupId: 30,
            PermitDate: DateTime.UtcNow,
            ProblemDescription: "Enterprise site 2 permit");

        var result = await service.CreateAsync(dto, 1);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.SubStationId);
    }

    [Fact]
    public async Task GetAllAsync_CommercialMode_FiltersByTechnicalSubStation()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        // Create permit in SubStation 1
        await service.CreateAsync(new CreateWorkPermitDto(
            SubStationId: 0,
            MaintenanceTypeLookupId: 10,
            DepartmentLookupId: 20,
            PriorityLookupId: 30,
            PermitDate: DateTime.UtcNow,
            ProblemDescription: "Commercial permit"), 1);

        // Manually insert permit in SubStation 2
        _db.WorkPermits.Add(new WorkPermit
        {
            PermitNumber = "WP-2026-OTH-0001",
            SubStationId = 2,
            MaintenanceTypeLookupId = 10,
            DepartmentLookupId = 20,
            PriorityLookupId = 30,
            StatusLookupId = 40,
            PermitDate = DateTime.UtcNow,
            ProblemDescription = "Other site permit",
            RequestedById = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();

        var paged = await service.GetAllAsync(1, 10);

        Assert.Single(paged.Items);
        Assert.Equal("Commercial Station", paged.Items[0].SubStationName);
    }
}
