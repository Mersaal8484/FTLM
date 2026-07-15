using FuelTrack.Application.DTOs.FuelMovements;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Tests.Services;

public class FuelMovementServiceTests : IDisposable
{
    private readonly FuelMovementService _service;
    private readonly FuelTrackDbContext _db;
    private readonly int _fuelLookupId;
    private readonly int _subStationId;

    public FuelMovementServiceTests()
    {
        var o = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"FuelMovementTestDb_{Guid.NewGuid()}")
            .Options;
        _db = new FuelTrackDbContext(o);

        _service = new FuelMovementService(_db, new StubHttpContextAccessor(), new StubUserScopeFilterService());

        _fuelLookupId = 1;
        _subStationId = 1;

        _db.Branches.Add(new Branch { Id = 1, Name = "Test Branch", Code = "TB" });
        _db.Stations.Add(new Station { Id = 1, BranchId = 1, Name = "Test Station", Code = "TS" });
        _db.LookupCategories.Add(new LookupCategory { Id = 1, Name = "Fuel", DisplayName = "Fuel" });
        _db.Lookups.Add(new Lookup { Id = _fuelLookupId, Name = "Diesel", CategoryId = 1, Code = "DIESEL" });
        _db.SubStations.Add(new SubStation { Id = _subStationId, StationId = 1, Name = "Test SubStation", Code = "TSS" });
        _db.SaveChanges();
    }

    private async Task<FuelTransaction> SeedMovement(bool isApproved = false)
    {
        var m = new FuelTransaction
        {
            SubStationId = _subStationId,
            FuelLookupId = _fuelLookupId,
            TransactionDate = DateTime.UtcNow,
            QuantityLiters = 100,
            Reference = "TST-001",
            IsApproved = isApproved,
            CreatedBy = 1,
            UpdatedBy = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.FuelTransactions.Add(m);
        await _db.SaveChangesAsync();
        return m;
    }

    [Fact]
    public async Task Create_DraftHasCorrectDefaultStatus()
    {
        var dto = new CreateFuelMovementDto(
            SubStationId: _subStationId,
            FuelLookupId: _fuelLookupId,
            TransactionTypeLookupId: null,
            TransactionDate: DateTime.UtcNow,
            QuantityLiters: 100,
            Reference: "TST-002",
            SupplierCompany: null,
            DriverName: null,
            DriverIdCard: null,
            TruckNumber: null,
            InvoiceNumber: null,
            InvoiceDate: null,
            InvoiceQuantity: null,
            WarehouseReceiptNumber: null,
            WarehouseReceiptDate: null,
            WarehouseQuantity: null,
            MeterId: null,
            PrevMeterVisualReading: null,
            CurrentMeterVisualReading: null,
            PrevMeterStandardReading: null,
            CurrentMeterStandardReading: null,
            SourceSubStationId: null,
            DestinationSubStationId: null,
            CorrectedFromId: null,
            CorrectionReason: null,
            Notes: null);

        var result = await _service.CreateAsync(dto, 1);

        Assert.False(result.IsApproved);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCreatedMovement()
    {
        var m = await SeedMovement();

        var result = await _service.GetByIdAsync(m.Id);

        Assert.NotNull(result);
        Assert.Equal(m.Id, result.Id);
    }

    [Fact]
    public async Task ApproveMovement_SetsIsApproved()
    {
        var m = await SeedMovement();

        var result = await _service.ApproveMovementAsync(m.Id, 1);

        Assert.True(result.IsApproved);
    }

    [Fact]
    public async Task RejectMovement_RemainsNotApproved()
    {
        var m = await SeedMovement();

        var result = await _service.RejectMovementAsync(m.Id, 1);

        Assert.False(result.IsApproved);
    }

    [Fact]
    public async Task Create_SubStationId_StoredCorrectly()
    {
        var dto = new CreateFuelMovementDto(
            SubStationId: _subStationId,
            FuelLookupId: _fuelLookupId,
            TransactionTypeLookupId: null,
            TransactionDate: DateTime.UtcNow,
            QuantityLiters: 250,
            Reference: "SUB-001",
            SupplierCompany: null,
            DriverName: null,
            DriverIdCard: null,
            TruckNumber: null,
            InvoiceNumber: null,
            InvoiceDate: null,
            InvoiceQuantity: null,
            WarehouseReceiptNumber: null,
            WarehouseReceiptDate: null,
            WarehouseQuantity: null,
            MeterId: null,
            PrevMeterVisualReading: null,
            CurrentMeterVisualReading: null,
            PrevMeterStandardReading: null,
            CurrentMeterStandardReading: null,
            SourceSubStationId: null,
            DestinationSubStationId: null,
            CorrectedFromId: null,
            CorrectionReason: null,
            Notes: null);

        var result = await _service.CreateAsync(dto, 1);

        Assert.Equal(_subStationId, result.SubStationId);
        Assert.Equal(250, result.QuantityLiters);
    }

    [Fact]
    public async Task Update_UpdatesFieldsCorrectly()
    {
        var m = await SeedMovement();

        var updateDto = new UpdateFuelMovementDto(
            FuelLookupId: null,
            TransactionTypeLookupId: null,
            TransactionDate: null,
            QuantityLiters: 500,
            Reference: "Updated Ref",
            SupplierCompany: null,
            DriverName: null,
            DriverIdCard: null,
            TruckNumber: null,
            InvoiceNumber: null,
            InvoiceDate: null,
            InvoiceQuantity: null,
            WarehouseReceiptNumber: null,
            WarehouseReceiptDate: null,
            WarehouseQuantity: null,
            MeterId: null,
            PrevMeterVisualReading: null,
            CurrentMeterVisualReading: null,
            PrevMeterStandardReading: null,
            CurrentMeterStandardReading: null,
            SourceSubStationId: null,
            DestinationSubStationId: null,
            CorrectedFromId: null,
            CorrectionReason: null,
            Notes: "Updated note");

        var result = await _service.UpdateAsync(m.Id, updateDto, 1);

        Assert.Equal(500, result.QuantityLiters);
        Assert.Equal("Updated Ref", result.Reference);
        Assert.Equal("Updated note", result.Notes);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
