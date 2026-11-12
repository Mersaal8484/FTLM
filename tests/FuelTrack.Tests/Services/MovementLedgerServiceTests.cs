using FuelTrack.Application.DTOs.Movements;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Tests.Services;

public sealed class MovementLedgerServiceTests : IDisposable
{
    private readonly FuelTrackDbContext _db;

    public MovementLedgerServiceTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"MovementLedger_{Guid.NewGuid():N}").Options);
        Seed();
    }

    [Fact]
    public async Task GetAsync_ExcludesMovementsOutsideUserScope()
    {
        var service = CreateService([10]);

        var result = await service.GetAsync(Filter());

        Assert.All(result.Items, item => Assert.Equal(10, item.SubStationId));
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetAsync_PagesUnifiedFuelAndOilRows()
    {
        var service = CreateService([10, 20]);

        var result = await service.GetAsync(Filter(page: 2, pageSize: 2));

        Assert.Equal(4, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public async Task GetAsync_AppliesKindStatusAndFreeTextFilters()
    {
        var service = CreateService([10, 20]);
        var filter = Filter() with { Kind = "fuel", Status = "approved", Search = "VISIBLE-APPROVED" };

        var result = await service.GetAsync(filter);

        var row = Assert.Single(result.Items);
        Assert.Equal("fuel", row.Kind);
        Assert.Equal("approved", row.Status);
        Assert.Equal("VISIBLE-APPROVED", row.Reference);
    }

    private MovementLedgerService CreateService(List<int> accessibleSites) => new(
        _db,
        new Scope(accessibleSites),
        new StubCurrentUserService { IsAdmin = false, UserId = 7 });

    private static MovementLedgerFilterDto Filter(int page = 1, int pageSize = 20) =>
        new(null, null, null, null, null, null, null, null, "date", "desc", page, pageSize);

    private void Seed()
    {
        _db.Branches.Add(new Branch { Id = 1, Name = "Branch", Code = "BR" });
        _db.Stations.AddRange(
            new Station { Id = 1, BranchId = 1, Name = "Station A", Code = "A" },
            new Station { Id = 2, BranchId = 1, Name = "Station B", Code = "B" });
        _db.SubStations.AddRange(
            new SubStation { Id = 10, StationId = 1, Name = "Visible Site", Code = "VS" },
            new SubStation { Id = 20, StationId = 2, Name = "Other Site", Code = "OS" });
        _db.LookupCategories.AddRange(
            new LookupCategory { Id = 1, Name = "Fuel", DisplayName = "Fuel" },
            new LookupCategory { Id = 2, Name = "Oil", DisplayName = "Oil" },
            new LookupCategory { Id = 3, Name = "MovementType", DisplayName = "MovementType" });
        _db.Lookups.AddRange(
            new Lookup { Id = 1, CategoryId = 1, Name = "Diesel", Code = "DIESEL" },
            new Lookup { Id = 2, CategoryId = 2, Name = "Engine Oil", Code = "OIL" },
            new Lookup { Id = 3, CategoryId = 3, Name = "Supply", Code = "Supply" });
        _db.FuelTransactions.AddRange(
            Fuel(1, 10, "VISIBLE-APPROVED", true),
            Fuel(2, 20, "OUTSIDE", false));
        _db.OilTransactions.AddRange(
            Oil(3, 10, "VISIBLE-OIL"),
            Oil(4, 20, "OTHER-OIL"));
        _db.SaveChanges();
    }

    private static FuelTransaction Fuel(int id, int siteId, string reference, bool approved) => new()
    {
        Id = id, SubStationId = siteId, FuelLookupId = 1, TransactionTypeLookupId = 3,
        TransactionDate = new DateTime(2026, 11, id, 8, 0, 0, DateTimeKind.Utc),
        QuantityLiters = id * 10, Reference = reference, IsApproved = approved,
        CreatedBy = 1, UpdatedBy = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
    };

    private static OilTransaction Oil(int id, int siteId, string reference) => new()
    {
        Id = id, SubStationId = siteId, OilLookupId = 2, TransactionTypeLookupId = 3,
        TransactionDate = new DateTime(2026, 11, id, 8, 0, 0, DateTimeKind.Utc),
        QuantityLiters = id * 10, Reference = reference,
        CreatedBy = 1, UpdatedBy = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
    };

    public void Dispose() => _db.Dispose();

    private sealed class Scope(List<int> ids) : IUserScopeFilterService
    {
        public Task<List<int>> GetAccessibleSubStationIdsAsync(int userId) => Task.FromResult(ids);
        public Task<List<int>> GetAccessibleStationIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetAccessibleBranchIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetAccessibleMeterIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetUserIdsWithAccessToSubStationAsync(int subStationId) => Task.FromResult(new List<int>());
        public Task<bool> HasAccessToSubStationAsync(int userId, int subStationId) => Task.FromResult(ids.Contains(subStationId));
        public Task<bool> HasFullAccessToStationAsync(int userId, int stationId) => Task.FromResult(false);
    }
}
