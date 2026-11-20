using FuelTrack.Application.Interfaces;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FuelTrack.Tests.Services;

public sealed class OilLedgerServiceTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly Mock<ICommercialStationModeService> _commercialMode = new();
    private readonly OilLedgerService _service;

    public OilLedgerServiceTests()
    {
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"OilLedgerTestDb_{Guid.NewGuid()}")
            .Options;
        _db = new FuelTrackDbContext(options);
        _commercialMode.Setup(x => x.IsCommercialModeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _service = new OilLedgerService(
            _db,
            Mock.Of<IUserScopeFilterService>(),
            new StubCurrentUserService { IsAdmin = false, UserId = 1 },
            _commercialMode.Object);
    }

    [Fact]
    public async Task GetMonthlyLedgerAsync_RejectsInvalidMonth()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.GetMonthlyLedgerAsync(2026, 13));
    }

    [Fact]
    public async Task GetMonthlyLedgerAsync_ReturnsEveryDayWithCarriedOpeningWhenThereAreNoMovements()
    {
        var branch = new Branch { Name = "B", Code = "B" };
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync();
        var station = new Station { BranchId = branch.Id, Name = "S", Code = "S" };
        _db.Stations.Add(station);
        await _db.SaveChangesAsync();
        var subStation = new SubStation { StationId = station.Id, Name = "SS", Code = "SS" };
        var outsideSubStation = new SubStation { StationId = station.Id, Name = "Outside", Code = "OUT" };
        _db.SubStations.Add(subStation);
        _db.SubStations.Add(outsideSubStation);
        await _db.SaveChangesAsync();
        var category = new LookupCategory { Name = "OilType", DisplayName = "Oil Type" };
        _db.LookupCategories.Add(category);
        await _db.SaveChangesAsync();
        var oilType = new Lookup { CategoryId = category.Id, Code = "SAE40", Name = "SAE 40" };
        _db.Lookups.Add(oilType);
        _db.Oils.Add(new Oil { SubStationId = subStation.Id, OilLookupId = oilType.Id, CurrentStock = 25m });
        _db.Oils.Add(new Oil { SubStationId = outsideSubStation.Id, OilLookupId = oilType.Id, CurrentStock = 99m });
        await _db.SaveChangesAsync();

        _commercialMode.Setup(x => x.GetScopeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommercialStationScope(branch.Id, station.Id, subStation.Id));

        var ledger = await _service.GetMonthlyLedgerAsync(2026, 1);

        Assert.Equal(31, ledger.Rows.Count);
        Assert.All(ledger.Rows, row =>
        {
            Assert.Equal(25m, row.Opening);
            Assert.Equal(25m, row.Closing);
        });
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
