using FuelTrack.Application.DTOs.Readings;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FuelTrack.Tests.Services;

public class ReadingServiceTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly ReadingService _service;
    private readonly int _genId = 1;
    private readonly int _userId = 1;
    private readonly int _draftLookupId;
    private readonly int _approvedLookupId;

    public ReadingServiceTests()
    {
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(databaseName: $"ReadingTestDb_{Guid.NewGuid()}")
            .Options;
        _db = new FuelTrackDbContext(options);

        var readingStatusCategory = new LookupCategory { Id = 1, Name = "ReadingStatus", DisplayName = "Reading Status" };
        _db.LookupCategories.Add(readingStatusCategory);

        var draftLookup = new Lookup { Id = 1, Name = "Draft", CategoryId = 1, Category = readingStatusCategory };
        var approvedLookup = new Lookup { Id = 2, Name = "Approved", CategoryId = 1, Category = readingStatusCategory };
        _db.Lookups.AddRange(draftLookup, approvedLookup);

        var transactionTypeCategory = new LookupCategory { Id = 2, Name = "TransactionType", DisplayName = "Transaction Type" };
        _db.LookupCategories.Add(transactionTypeCategory);
        var consumptionLookup = new Lookup { Id = 3, Name = "Consumption", CategoryId = 2, Category = transactionTypeCategory };
        _db.Lookups.Add(consumptionLookup);

        var fuelCategory = new LookupCategory { Id = 3, Name = "FuelType", DisplayName = "Fuel Type" };
        _db.LookupCategories.Add(fuelCategory);
        var fuelLookup = new Lookup { Id = 10, Name = "Diesel", CategoryId = 3, Category = fuelCategory };
        _db.Lookups.Add(fuelLookup);

        var oilCategory = new LookupCategory { Id = 4, Name = "OilType", DisplayName = "Oil Type" };
        _db.LookupCategories.Add(oilCategory);
        var oilLookup = new Lookup { Id = 11, Name = "Engine Oil", CategoryId = 4, Category = oilCategory };
        _db.Lookups.Add(oilLookup);

        _db.SaveChanges();

        _draftLookupId = _db.Lookups.First(l => l.Name == "Draft").Id;
        _approvedLookupId = _db.Lookups.First(l => l.Name == "Approved").Id;

        _db.Generators.Add(new Generator { Id = _genId, Name = "Gen 1", SerialNumber = "G1", SubStationId = 1, ActualPowerKW = 100 });
        _db.SaveChanges();

        var mockScopeFilter = new Mock<IUserScopeFilterService>();
        mockScopeFilter.Setup(s => s.GetAccessibleSubStationIdsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<int>());
        mockScopeFilter.Setup(s => s.HasAccessToSubStationAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        var mockLogger = new Mock<ILogger<ReadingService>>();

        _service = new ReadingService(_db, mockScopeFilter.Object, mockLogger.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_CreatesDraftReading()
    {
        var dto = new CreateReadingDto(
            _genId,
            DateTime.UtcNow,
            100,
            150,
            50,
            55,
            10,
            10m,
            11,
            null,
            null
        );

        var result = await _service.CreateAsync(dto, _userId);

        Assert.NotNull(result);
        Assert.Equal(_draftLookupId, result.ReadingStatusLookupId);
        Assert.Equal(50, result.PrevRunningHours);

        var inDb = await _db.DailyReadings.FindAsync(result.Id);
        Assert.NotNull(inDb);
        Assert.Equal(_draftLookupId, inDb.ReadingStatusLookupId);
    }

    [Fact]
    public async Task CreateAsync_WhenGeneratorHasLinkedController_RecordsControllerId()
    {
        _db.Controllers.Add(new Controller { Id = 5, Name = "CTRL-1", GeneratorId = _genId });
        await _db.SaveChangesAsync();

        var dto = new CreateReadingDto(_genId, DateTime.UtcNow, 100, 150, 50, 55, 10, 10m, 11, null, null);

        var result = await _service.CreateAsync(dto, _userId);

        var inDb = await _db.DailyReadings.FindAsync(result.Id);
        Assert.NotNull(inDb);
        Assert.Equal(5, inDb!.ControllerId);
        Assert.Equal("CTRL-1", result.ControllerName);
    }

    [Fact]
    public async Task CreateAsync_WhenGeneratorHasNoLinkedController_ControllerIdIsNull()
    {
        var dto = new CreateReadingDto(_genId, DateTime.UtcNow, 100, 150, 50, 55, 10, 10m, 11, null, null);

        var result = await _service.CreateAsync(dto, _userId);

        var inDb = await _db.DailyReadings.FindAsync(result.Id);
        Assert.NotNull(inDb);
        Assert.Null(inDb!.ControllerId);
        Assert.Null(result.ControllerName);
    }

    [Fact]
    public async Task CreateAsync_DuplicateDraft_ThrowsInvalidOperationException()
    {
        var dto = new CreateReadingDto(_genId, DateTime.UtcNow.Date, 100, 150, 50, 55, 10, 10m, 11, null, null);

        await _service.CreateAsync(dto, _userId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto, _userId));
    }

    [Fact]
    public async Task ApproveAsync_DraftReading_SetsToApproved()
    {
        var reading = new DailyReading
        {
            GeneratorId = _genId,
            ReadingDate = DateTime.UtcNow,
            ReadingStatusLookupId = _draftLookupId,
            CreatedBy = 1,
            PrevEnergyKWh = 100,
            CurrentEnergyKWh = 150,
            PrevRunningHours = 50,
            CurrentRunningHours = 55,
            FuelLookupId = 10,
            StandardFuelRate = 10m,
            OilLookupId = 11,
        };
        _db.DailyReadings.Add(reading);
        await _db.SaveChangesAsync();

        var dto = new ApproveReadingDto(1, null);
        var result = await _service.ApproveAsync(reading.Id, dto);

        Assert.Equal(_approvedLookupId, result.ReadingStatusLookupId);
    }

    [Fact]
    public async Task ApproveAsync_NonDraftReading_ThrowsInvalidOperationException()
    {
        var reading = new DailyReading
        {
            GeneratorId = _genId,
            ReadingDate = DateTime.UtcNow,
            ReadingStatusLookupId = _approvedLookupId,
            CreatedBy = 1,
            PrevEnergyKWh = 100,
            CurrentEnergyKWh = 150,
            PrevRunningHours = 50,
            CurrentRunningHours = 55,
            FuelLookupId = 10,
            StandardFuelRate = 10m,
            OilLookupId = 11,
        };
        _db.DailyReadings.Add(reading);
        await _db.SaveChangesAsync();

        var dto = new ApproveReadingDto(1, null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApproveAsync(reading.Id, dto));
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
