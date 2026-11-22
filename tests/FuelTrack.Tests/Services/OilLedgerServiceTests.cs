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
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.GetMonthlyLedgerAsync(2026, 13, null));
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

        var ledger = await _service.GetMonthlyLedgerAsync(2026, 1, null);

        Assert.Equal(31, ledger.Rows.Count);
        Assert.All(ledger.Rows, row =>
        {
            Assert.Equal(25m, row.Opening);
            Assert.Equal(25m, row.Closing);
        });
    }

    [Fact]
    public async Task GetMonthlyLedgerAsync_ConsumptionTransaction_ReducesClosingBalanceAndAppearsInDayTransactions()
    {
        var branch = new Branch { Name = "B", Code = "B" };
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync();
        var station = new Station { BranchId = branch.Id, Name = "S", Code = "S" };
        _db.Stations.Add(station);
        await _db.SaveChangesAsync();
        var subStation = new SubStation { StationId = station.Id, Name = "SS", Code = "SS" };
        _db.SubStations.Add(subStation);
        await _db.SaveChangesAsync();
        var category = new LookupCategory { Name = "OilType", DisplayName = "Oil Type" };
        var txTypeCategory = new LookupCategory { Name = "OilTransactionType", DisplayName = "Oil Transaction Type" };
        _db.LookupCategories.AddRange(category, txTypeCategory);
        await _db.SaveChangesAsync();
        var oilType = new Lookup { CategoryId = category.Id, Code = "15W40", Name = "15W-40" };
        var consumptionType = new Lookup { CategoryId = txTypeCategory.Id, Code = "Consumption", Name = "استهلاك" };
        _db.Lookups.AddRange(oilType, consumptionType);
        _db.Oils.Add(new Oil { SubStationId = subStation.Id, OilLookupId = oilType.Id, CurrentStock = 100m });
        await _db.SaveChangesAsync();

        _commercialMode.Setup(x => x.GetScopeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommercialStationScope(branch.Id, station.Id, subStation.Id));

        var txDate = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        _db.OilTransactions.Add(new OilTransaction
        {
            SubStationId = subStation.Id,
            OilLookupId = oilType.Id,
            TransactionTypeLookupId = consumptionType.Id,
            TransactionDate = txDate,
            QuantityLiters = 20.0,
            Reference = "SRV-101",
            IsApproved = true
        });
        await _db.SaveChangesAsync();

        var ledger = await _service.GetMonthlyLedgerAsync(2026, 1, null);

        var jan14 = ledger.Rows.First(r => r.Date == new DateOnly(2026, 1, 14));
        var jan15 = ledger.Rows.First(r => r.Date == new DateOnly(2026, 1, 15));
        var jan16 = ledger.Rows.First(r => r.Date == new DateOnly(2026, 1, 16));

        Assert.Equal(100m, jan14.Closing);
        Assert.Equal(100m, jan15.Opening);
        Assert.Equal(20m, jan15.Outbound);
        Assert.Equal(80m, jan15.Closing);
        Assert.Equal(80m, jan16.Opening);
        Assert.Equal(80m, jan16.Closing);

        var dayTxs = await _service.GetDayTransactionsAsync(new DateOnly(2026, 1, 15), null);
        var dayTx = Assert.Single(dayTxs);
        Assert.Equal("Consumption", dayTx.TypeCode);
        Assert.Equal(20m, dayTx.QuantityLiters);
        Assert.Equal("SRV-101", dayTx.Reference);

        var balance = await _service.GetCurrentBalanceAsync(subStation.Id, oilType.Id);
        Assert.Equal(80m, balance);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
