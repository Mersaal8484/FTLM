using FuelTrack.Application.DTOs.FuelTanks;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Validators.FuelTanks;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FuelTrack.Tests.Services;

/// <summary>
/// Verifies the owner-confirmed rule: commercial station tanks (main and daily)
/// are VERTICAL cylinders with a CONICAL BOTTOM (TankShape.VerticalCylinderWithConicalBottom = 0).
/// Horizontal conical tanks (Shape = 1) are rejected by API validation and service logic.
/// </summary>
public sealed class CommercialTankVerticalGeometryTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly CommercialStationModeService _commercialMode;
    private readonly StubCurrentUserService _currentUser;
    private readonly StubUserScopeFilterService _scopeFilter;
    private readonly FuelTankService _tankService;
    private readonly TankCalibrationService _calibrationService;

    private const decimal Diameter = 200m;
    private const decimal CylinderHeight = 150m;
    private const decimal ConeHeight = 50m;

    public CommercialTankVerticalGeometryTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"CommercialVerticalTank_{Guid.NewGuid()}").Options);

        _commercialMode = new CommercialStationModeService(_db);
        _currentUser = new StubCurrentUserService { UserId = 1, IsAdmin = true };
        _scopeFilter = new StubUserScopeFilterService();
        _tankService = new FuelTankService(_db, _scopeFilter, _currentUser, _commercialMode);
        _calibrationService = new TankCalibrationService(_db, _scopeFilter, _currentUser, _commercialMode);

        // Seed basic dependencies
        var branch = new Branch { Id = 1, Code = "BR-01", Name = "Commercial Branch", IsActive = true };
        var station = new Station { Id = 1, BranchId = 1, Code = "ST-01", Name = "Commercial Station", IsActive = true };
        var subStation = new SubStation { Id = 1, StationId = 1, Code = "SITE-01", Name = "Commercial Site", IsActive = true };
        var fuelCategory = new LookupCategory { Id = 1, Name = "FuelType", DisplayName = "Fuel types", IsActive = true };
        var fuelLookup = new Lookup
        {
            Id = 1,
            CategoryId = fuelCategory.Id,
            Category = fuelCategory,
            Code = "DIESEL",
            Name = "Diesel",
            IsActive = true
        };

        _db.Branches.Add(branch);
        _db.Stations.Add(station);
        _db.SubStations.Add(subStation);
        _db.LookupCategories.Add(fuelCategory);
        _db.Lookups.Add(fuelLookup);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private async Task EnableCommercialModeAsync()
    {
        _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
        {
            Key = CommercialStationModeService.FeatureKey,
            IsEnabled = true,
            Value = CommercialStationModeService.CommercialModeValue,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task CommercialTank_CreatedWithoutShape_GetsShape0_VerticalConical()
    {
        await EnableCommercialModeAsync();

        var dto = new CreateFuelTankDto(
            SubStationId: 1,
            FuelLookupId: 1,
            Name: "Commercial Main Storage",
            Code: "TK-MAIN-01",
            CapacityLiters: 10000,
            MinAlertLiters: 500,
            DeadStockLiters: 100,
            ActualStockLiters: 5000,
            HasMeter: false,
            MeterReading: null,
            MeterReadingActual: null,
            Notes: "Commercial main tank test",
            IsActive: true,
            TankRole: TankRole.Main,
            GeneratorId: null,
            ParentTankId: null,
            Shape: null // Omitted shape
        );

        var result = await _tankService.CreateAsync(dto, 1);

        // Commercial tank created without shape defaults to VerticalCylinderWithConicalBottom (0)
        Assert.NotNull(result.Shape);
        Assert.Equal(TankShape.VerticalCylinderWithConicalBottom, result.Shape);
        Assert.Equal(0, (int)result.Shape.Value);

        var tankInDb = await _db.FuelTanks.FindAsync(result.Id);
        Assert.NotNull(tankInDb);
        Assert.Equal(TankShape.VerticalCylinderWithConicalBottom, tankInDb.Shape);
    }

    [Fact]
    public void Validator_Rejects_Shape1_OnCreate()
    {
        var validator = new CreateFuelTankDtoValidator();
        var dto = new CreateFuelTankDto(
            SubStationId: 1,
            FuelLookupId: 1,
            Name: "Invalid Horizontal Tank",
            Code: "TK-INVALID",
            CapacityLiters: 5000,
            MinAlertLiters: 100,
            DeadStockLiters: null,
            ActualStockLiters: null,
            HasMeter: false,
            MeterReading: null,
            MeterReadingActual: null,
            Notes: null,
            Shape: (TankShape)1 // Shape = 1
        );

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateFuelTankDto.Shape));
    }

    [Fact]
    public void Validator_Rejects_Shape1_OnUpdate()
    {
        var validator = new UpdateFuelTankDtoValidator();
        var dto = new UpdateFuelTankDto(
            SubStationId: null,
            FuelLookupId: null,
            Name: "Updated Tank",
            Code: null,
            CapacityLiters: null,
            MinAlertLiters: null,
            DeadStockLiters: null,
            ActualStockLiters: null,
            HasMeter: null,
            MeterReading: null,
            MeterReadingActual: null,
            Notes: null,
            IsActive: null,
            Shape: (TankShape)1 // Shape = 1
        );

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateFuelTankDto.Shape));
    }

    [Fact]
    public async Task FuelTankService_CreateAsync_Rejects_Shape1()
    {
        await EnableCommercialModeAsync();

        var dto = new CreateFuelTankDto(
            SubStationId: 1,
            FuelLookupId: 1,
            Name: "Should Be Rejected",
            Code: "TK-REJECT",
            CapacityLiters: 5000,
            MinAlertLiters: 100,
            DeadStockLiters: null,
            ActualStockLiters: null,
            HasMeter: false,
            MeterReading: null,
            MeterReadingActual: null,
            Notes: null,
            Shape: (TankShape)1
        );

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tankService.CreateAsync(dto, 1));
        Assert.Contains("Unsupported tank shape", ex.Message);
    }

    [Fact]
    public async Task CommercialTank_ReadsIgnoreTheClientSubStationId()
    {
        _db.FuelTanks.Add(new FuelTank
        {
            Id = 44,
            SubStationId = 1,
            FuelLookupId = 1,
            Name = "Technical-scope tank",
            Code = "TK-SCOPE-44",
            CapacityLiters = 1000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        await EnableCommercialModeAsync();

        var tanks = await _tankService.GetBySubStationAsync(999);
        var tank = await _tankService.GetByIdAsync(44);

        Assert.Single(tanks);
        Assert.Equal(44, tanks[0].Id);
        Assert.NotNull(tank);
        Assert.Equal(1, tank.SubStationId);
    }

    [Fact]
    public async Task FuelTankService_UpdateAsync_Rejects_Shape1()
    {
        var existing = new FuelTank
        {
            Id = 10,
            SubStationId = 1,
            FuelLookupId = 1,
            Name = "Existing Tank",
            Code = "TK-10",
            CapacityLiters = 5000,
            Shape = TankShape.VerticalCylinderWithConicalBottom,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.FuelTanks.Add(existing);
        await _db.SaveChangesAsync();

        var updateDto = new UpdateFuelTankDto(
            SubStationId: null,
            FuelLookupId: null,
            Name: null,
            Code: null,
            CapacityLiters: null,
            MinAlertLiters: null,
            DeadStockLiters: null,
            ActualStockLiters: null,
            HasMeter: null,
            MeterReading: null,
            MeterReadingActual: null,
            Notes: null,
            IsActive: null,
            Shape: (TankShape)1
        );

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tankService.UpdateAsync(10, updateDto, 1));
        Assert.Contains("Unsupported tank shape", ex.Message);
    }

    [Fact]
    public async Task TankCalibrationService_UpsertAsync_Rejects_Shape1()
    {
        var existing = new FuelTank
        {
            Id = 11,
            SubStationId = 1,
            FuelLookupId = 1,
            Name = "Existing Tank 11",
            Code = "TK-11",
            CapacityLiters = 5000,
            Shape = TankShape.VerticalCylinderWithConicalBottom,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.FuelTanks.Add(existing);
        await _db.SaveChangesAsync();

        var calDto = new UpdateTankCalibrationDto(
            DiameterCm: 200m,
            CylinderHeightCm: 150m,
            ConeHeightCm: 50m,
            MultiplicationFactor: 1m,
            Shape: (TankShape)1
        );

        var result = await _calibrationService.UpsertAsync(11, calDto, 1);
        Assert.False(result.IsSuccess);
        Assert.Contains("invalid", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(25, 65.45)]
    [InlineData(50, 523.60)]
    [InlineData(100, 2094.40)]
    [InlineData(200, 5235.99)]
    public void VerticalConicalFixtures_D200_Hc50_Hcyl150_AreExact(decimal level, decimal expectedLitres)
    {
        var litres = TankGeometry.CalculateLitres(
            TankShape.VerticalCylinderWithConicalBottom,
            Diameter, CylinderHeight, ConeHeight, 1m, level);

        Assert.Equal(expectedLitres, litres, 2);
    }

    [Fact]
    public void VerticalConicalFixture_Multiplier1Point1_AtLevel100_IsExact()
    {
        var litres = TankGeometry.CalculateLitres(
            TankShape.VerticalCylinderWithConicalBottom,
            Diameter, CylinderHeight, ConeHeight, 1.1m, 100m);

        Assert.Equal(2303.83m, litres, 2);
    }

    [Fact]
    public void DeadStockSubtraction_WorksOnVerticalShape_InCommercialMode()
    {
        // Tank with vertical conical shape: D=200, Hc=50, Hcyl=150
        // Dead stock input at 25 cm height in the cone apex
        var tankWithCmDeadStock = new FuelTank
        {
            DiameterCm = Diameter,
            CylinderHeightCm = CylinderHeight,
            ConeHeightCm = ConeHeight,
            MultiplicationFactor = 1m,
            Shape = TankShape.VerticalCylinderWithConicalBottom,
            DeadStockLevelCm = 25m,
            DeadStockInputUnit = "cm",
            DeadStockLiters = null
        };

        var resolvedLitresFromCm = TankVolumeService.DeadStockLitres(tankWithCmDeadStock);
        Assert.Equal(65.45m, resolvedLitresFromCm, 2);

        // When reading is at 100 cm (2094.40 L)
        var totalFuelAt100 = TankGeometry.CalculateLitres(
            TankShape.VerticalCylinderWithConicalBottom,
            Diameter, CylinderHeight, ConeHeight, 1m, 100m);
        Assert.Equal(2094.40m, totalFuelAt100, 2);

        var usableFuel = totalFuelAt100 - resolvedLitresFromCm;
        Assert.Equal(2028.95m, usableFuel, 2);

        // When dead stock is provided directly in litres (65.45 L)
        var tankWithLitreDeadStock = new FuelTank
        {
            DiameterCm = Diameter,
            CylinderHeightCm = CylinderHeight,
            ConeHeightCm = ConeHeight,
            MultiplicationFactor = 1m,
            Shape = TankShape.VerticalCylinderWithConicalBottom,
            DeadStockLiters = 65.45,
            DeadStockInputUnit = "litres",
            DeadStockLevelCm = null
        };

        var resolvedLitresFromLitre = TankVolumeService.DeadStockLitres(tankWithLitreDeadStock);
        Assert.Equal(65.45m, resolvedLitresFromLitre, 2);
    }
}
