using FuelTrack.Application.DTOs.Controllers;
using FuelTrack.Application.DTOs.Generators;
using FuelTrack.Application.Interfaces;
using FuelTrack.Application.Validators.Generators;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.MultiTenancy;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FuelTrack.Tests.Services;

/// <summary>
/// Phase C3.2 corrections test suite.
/// Verifies:
/// 1. Enterprise behavior unchanged (NominalPower & FuelConsumptionRate preserved).
/// 2. Commercial mode null inputs persist safe defaults (0) without breaking consumers.
/// 3. Server-side enforcement for Controller operating hours (non-negative and >= approved reading).
/// 4. Cross-tenant / out-of-scope query denial.
/// 5. Tenant upgrade 010 parity with EF migration 20261116120000.
/// </summary>
public sealed class CommercialC3_2Tests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly StubCurrentUserService _currentUser;
    private readonly StubUserScopeFilterService _scopeFilter;
    private readonly GeneratorService _genService;
    private readonly ControllerService _ctrlService;

    public CommercialC3_2Tests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"CommercialC3_2_{Guid.NewGuid()}").Options);

        _currentUser = new StubCurrentUserService { UserId = 1, IsAdmin = false };
        _scopeFilter = new StubUserScopeFilterService();
        _genService = new GeneratorService(_db, _scopeFilter, _currentUser);
        _ctrlService = new ControllerService(_db);

        // Seed basic SubStation
        var branch = new Branch { Id = 1, Code = "BR-01", Name = "Branch 1", IsActive = true };
        var station = new Station { Id = 1, BranchId = 1, Code = "ST-01", Name = "Station 1", IsActive = true };
        var subStation = new SubStation { Id = 1, StationId = 1, Code = "SITE-01", Name = "Site 1", IsActive = true };
        _db.Branches.Add(branch);
        _db.Stations.Add(station);
        _db.SubStations.Add(subStation);
        var fuelCategory = new LookupCategory { Id = 50, Name = "FuelType", DisplayName = "Fuel type" };
        _db.LookupCategories.Add(fuelCategory);
        _db.Lookups.Add(new Lookup { Id = 500, CategoryId = fuelCategory.Id, Name = "Diesel", Code = "Diesel", IsActive = true });
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task Enterprise_GeneratorCreate_PreservesNominalPowerAndFuelRate()
    {
        var dto = new CreateGeneratorDto(
            SubStationId: 1,
            Name: "Enterprise Gen 1",
            SerialNumber: "SN-ENT-001",
            Model: "Caterpillar 3516",
            Manufacturer: "CAT",
            NominalPower: 1600.0,
            NominalPowerUnitLookupId: null,
            ActualPowerKW: 1400,
            FuelConsumptionRatePerHour: 0.28m,
            OwnerName: "Enterprise Owner",
            GeneratorTypeLookupId: null,
            GeneratorTypeLookupName: null,
            VoltageLookupId: null,
            VoltageLookupName: null,
            FrequencyLookupId: null,
            FrequencyLookupName: null,
            PowerFactor: 0.8,
            OwnershipTypeLookupId: null,
            OwnershipTypeLookupName: null,
            StatusLookupId: null,
            StatusLookupName: null,
            InstallationDate: null,
            Notes: null,
            IsActive: true
        );

        var result = await _genService.CreateAsync(dto, 1);
        Assert.True(result.IsSuccess);

        var created = await _db.Generators.FirstOrDefaultAsync(g => g.SerialNumber == "SN-ENT-001");
        Assert.NotNull(created);
        Assert.Equal(1600.0, created.NominalPower);
        Assert.Equal(0.28m, created.FuelConsumptionRatePerHour);
        Assert.Equal("Enterprise Owner", created.OwnerName);
    }

    [Fact]
    public async Task Commercial_GeneratorCreate_NullFields_PersistsZeroDefaultsWithoutError()
    {
        // In commercial mode, UI sends null for NominalPower, FuelConsumptionRatePerHour, and OwnerName
        var dto = new CreateGeneratorDto(
            SubStationId: 1,
            Name: "Commercial Gen 1",
            SerialNumber: "SN-COMM-001",
            Model: "Cummins QSK60",
            Manufacturer: "Cummins",
            NominalPower: null,
            NominalPowerUnitLookupId: null,
            ActualPowerKW: 1200,
            FuelConsumptionRatePerHour: null,
            OwnerName: null,
            GeneratorTypeLookupId: null,
            GeneratorTypeLookupName: null,
            VoltageLookupId: null,
            VoltageLookupName: null,
            FrequencyLookupId: null,
            FrequencyLookupName: null,
            PowerFactor: null,
            OwnershipTypeLookupId: null,
            OwnershipTypeLookupName: null,
            StatusLookupId: null,
            StatusLookupName: null,
            InstallationDate: null,
            Notes: null,
            IsActive: true
        );

        var result = await _genService.CreateAsync(dto, 1);
        Assert.True(result.IsSuccess);

        var created = await _db.Generators.FirstOrDefaultAsync(g => g.SerialNumber == "SN-COMM-001");
        Assert.NotNull(created);
        // DB columns are NOT NULL, server-side defaults 0 are safely applied
        Assert.Equal(0.0, created.NominalPower);
        Assert.Equal(0.0m, created.FuelConsumptionRatePerHour);
        Assert.Null(created.OwnerName);
    }

    [Fact]
    public async Task Commercial_GeneratorCreate_ResolvesTheTechnicalSiteAndIgnoresClientSiteId()
    {
        var commercialMode = new CommercialStationModeService(_db);
        var activation = await commercialMode.ActivateAsync(new("Commercial Station", "CS-01"), 1);
        Assert.True(activation.IsSuccess);

        var technicalSite = Assert.Single(await _db.SubStations.ToListAsync());
        var tank = new FuelTank
        {
            SubStationId = technicalSite.Id,
            FuelLookupId = 500,
            Name = "Scoped daily tank",
            Code = "DT-SCOPED",
            CapacityLiters = 1000,
            ActualStockLiters = 500,
            TankRole = TankRole.Daily,
            IsActive = true
        };
        _db.FuelTanks.Add(tank);
        await _db.SaveChangesAsync();

        var service = new GeneratorService(_db, _scopeFilter, _currentUser, commercialMode);
        var result = await service.CreateAsync(new CreateGeneratorDto(
            SubStationId: 999,
            Name: "Scoped generator",
            SerialNumber: "SCOPED-001",
            Model: "Model",
            Manufacturer: "Manufacturer",
            ActualPowerKW: 100,
            DailyTankId: tank.Id), 1);

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(await _db.Generators.Where(generator => generator.SerialNumber == "SCOPED-001").ToListAsync());
        Assert.Equal(technicalSite.Id, saved.SubStationId);
        Assert.Single(await _db.FuelTanks.Where(t => t.GeneratorId == saved.Id && t.TankRole == TankRole.Daily).ToListAsync());
    }

    [Fact]
    public async Task Commercial_GeneratorCreate_WithoutDailyTank_IsRejected()
    {
        var commercialMode = new CommercialStationModeService(_db);
        Assert.True((await commercialMode.ActivateAsync(new("Commercial Station", "CS-01"), 1)).IsSuccess);
        var service = new GeneratorService(_db, _scopeFilter, _currentUser, commercialMode);

        var result = await service.CreateAsync(new CreateGeneratorDto(999, "No tank", "NO-TANK", null, null), 1);

        Assert.False(result.IsSuccess);
        Assert.Contains("daily tank", result.Error!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(_db.Generators, generator => generator.SerialNumber == "NO-TANK");
    }

    [Fact]
    public async Task Validator_CreateGeneratorDto_AcceptsNull_RejectsNegativeValues()
    {
        var validator = new CreateGeneratorDtoValidator();

        // Null is accepted for commercial mode
        var validNullDto = new CreateGeneratorDto(
            SubStationId: 1,
            Name: "Gen A",
            SerialNumber: "SN-A",
            Model: "Mod",
            Manufacturer: null,
            NominalPower: null,
            NominalPowerUnitLookupId: null,
            ActualPowerKW: 0,
            FuelConsumptionRatePerHour: null,
            OwnerName: null,
            GeneratorTypeLookupId: null,
            GeneratorTypeLookupName: null,
            VoltageLookupId: null,
            VoltageLookupName: null,
            FrequencyLookupId: null,
            FrequencyLookupName: null,
            PowerFactor: null,
            OwnershipTypeLookupId: null,
            OwnershipTypeLookupName: null,
            StatusLookupId: null,
            StatusLookupName: null,
            InstallationDate: null,
            Notes: null,
            IsActive: true
        );
        var nullResult = await validator.ValidateAsync(validNullDto);
        Assert.True(nullResult.IsValid);

        // Negative value is rejected
        var invalidNegativeDto = validNullDto with { NominalPower = -50.0 };
        var negativeResult = await validator.ValidateAsync(invalidNegativeDto);
        Assert.False(negativeResult.IsValid);
        Assert.Contains(negativeResult.Errors, e => e.PropertyName == nameof(CreateGeneratorDto.NominalPower));
    }

    [Fact]
    public async Task ControllerService_CreateAsync_RejectsNegativeOperatingHours()
    {
        var dto = new CreateControllerDto(
            GeneratorId: null,
            Name: "Controller 1",
            Code: "CTRL-01",
            ControllerType: "DSE8610",
            IpAddress: null,
            Port: 502,
            ModbusUnitId: 1,
            SerialNumber: "SN-CTRL-01",
            FirmwareVersion: null,
            EnergyReading: 0,
            OperatingHours: -5.0, // Negative operating hours
            RegisterType: "FC03",
            AutoConnect: false,
            EnergyUnit: "kWh"
        );

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _ctrlService.CreateAsync(dto));
        Assert.Contains("non-negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ControllerService_UpdateAsync_RejectsOperatingHours_LessThanApprovedDailyClosing()
    {
        // 1. Create generator
        var gen = new Generator
        {
            Id = 50,
            SubStationId = 1,
            Name = "Gen 50",
            SerialNumber = "GEN-50",
            Model = "M50",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Generators.Add(gen);

        // 2. Create controller with 100 hours
        var ctrl = new Domain.Models.Controller
        {
            Id = 10,
            GeneratorId = 50,
            Name = "Ctrl 10",
            Code = "CTRL-10",
            ControllerType = "DSE8610",
            OperatingHours = 100.0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Controllers.Add(ctrl);

        // 3. Seed an approved daily-closing reading for generator running hours at 120.0
        var approvedReading = new MeterReading
        {
            Id = 1,
            SubjectType = MeterReadingSubjectType.GeneratorRunningHours,
            SubjectId = 50,
            Kind = MeterReadingKind.DailyClosing,
            Status = OperationalReadingStatus.Approved,
            RawValue = 120.0m,
            MultiplierSnapshot = 1.0m,
            CapturedAtUtc = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.MeterReadings.Add(approvedReading);
        await _db.SaveChangesAsync();

        // 4. Try updating controller operating hours to 110.0 (< approved 120.0)
        var updateDto = new CreateControllerDto(
            GeneratorId: 50,
            Name: "Ctrl 10",
            Code: "CTRL-10",
            ControllerType: "DSE8610",
            IpAddress: null,
            Port: 502,
            ModbusUnitId: 1,
            SerialNumber: "SN-10",
            FirmwareVersion: null,
            EnergyReading: 0,
            OperatingHours: 110.0, // Less than 120.0
            RegisterType: "FC03",
            AutoConnect: false,
            EnergyUnit: "kWh"
        );

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _ctrlService.UpdateAsync(10, updateDto));
        Assert.Contains("approved daily closing", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CrossTenant_OutOfScopeSubStation_DeniedFromGeneratorQuery()
    {
        // Substation 99 is outside user scope
        var foreignSub = new SubStation { Id = 99, StationId = 1, Code = "SITE-99", Name = "Foreign Site", IsActive = true };
        _db.SubStations.Add(foreignSub);

        var foreignGen = new Generator
        {
            Id = 99,
            SubStationId = 99,
            Name = "Foreign Generator",
            SerialNumber = "SN-FOREIGN-99",
            Model = "Cat",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Generators.Add(foreignGen);
        await _db.SaveChangesAsync();

        // StubUserScopeFilterService returns only SubStation 1 as accessible
        var restrictedScopeFilter = new RestrictedScopeFilter(new List<int> { 1 });
        var scopedService = new GeneratorService(_db, restrictedScopeFilter, _currentUser);

        var generators = await scopedService.GetAllAsync(_currentUser.UserId!.Value);

        // The generator in substation 99 must not be returned to this user.
        Assert.DoesNotContain(generators, generator => generator.Id == foreignGen.Id);
    }

    [Fact]
    public async Task TenantUpgrade010_RegisteredInCatalog_WithCorrectVersionAndSql()
    {
        var upgrades = await TenantSchemaUpgradeCatalog.GetCurrentAsync();
        var upgrade10 = upgrades.FirstOrDefault(u => u.Version == 10);

        Assert.NotNull(upgrade10);
        Assert.Equal(10, upgrade10.Version);
        Assert.NotNull(upgrade10.Sql);
        Assert.Contains("ALTER TABLE \"FuelTanks\"", upgrade10.Sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"TankRole\" integer", upgrade10.Sql);
        Assert.Contains("CREATE INDEX IF NOT EXISTS \"IX_FuelTanks_TankRole\"", upgrade10.Sql);
        Assert.Contains("DROP COLUMN IF EXISTS \"TankRole\"", upgrade10.Sql); // Rollback plan documented
    }

    [Fact]
    public async Task TenantUpgrade013_RegisteredInCatalog_WithFilteredMainTankIndex()
    {
        var upgrade = (await TenantSchemaUpgradeCatalog.GetCurrentAsync()).Single(x => x.Version == 13);

        Assert.Contains("DROP INDEX IF EXISTS \"IX_FuelTanks_SubStationId_FuelLookupId\"", upgrade.Sql);
        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_FuelTanks_SubStationId_FuelLookupId\"", upgrade.Sql);
        Assert.Contains("\"DeletedAt\" IS NULL AND \"GeneratorId\" IS NULL", upgrade.Sql);
    }

    [Fact]
    public async Task TenantUpgrade014_RegisteredInCatalog_AddsOilTransactionIdColumnAndIndex()
    {
        var upgrade = (await TenantSchemaUpgradeCatalog.GetCurrentAsync()).Single(x => x.Version == 14);

        Assert.NotNull(upgrade);
        Assert.Equal(14, upgrade.Version);
        Assert.NotNull(upgrade.Sql);
        Assert.Contains("ALTER TABLE \"ServiceReplacementRecords\"", upgrade.Sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"OilTransactionId\" integer", upgrade.Sql);
        Assert.Contains("CREATE INDEX IF NOT EXISTS \"IX_ServiceReplacementRecords_OilTransactionId\"", upgrade.Sql);
    }

    [Fact]
    public async Task TenantUpgrade016_RegisteredInCatalog_AddsCalibrationModeAndHeightColumns()
    {
        var upgrade = (await TenantSchemaUpgradeCatalog.GetCurrentAsync()).Single(x => x.Version == 16);

        Assert.NotNull(upgrade);
        Assert.Equal(16, upgrade.Version);
        Assert.Contains("ALTER TABLE \"FuelTanks\"", upgrade.Sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"CalibrationMode\" integer NOT NULL DEFAULT 0", upgrade.Sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"LitresPerCm\" numeric(18,6)", upgrade.Sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"CapacityHeightCm\" numeric(18,4)", upgrade.Sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"CapacityInputUnit\" varchar(16) NOT NULL DEFAULT 'litres'", upgrade.Sql);
        Assert.Contains("ALTER TABLE \"TankLevelReadings\"", upgrade.Sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"CalibrationModeSnapshot\" integer NOT NULL DEFAULT 0", upgrade.Sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"LitresPerCmSnapshot\" numeric(18,6)", upgrade.Sql);
    }

    [Fact]
    public async Task TenantUpgrade017_RegisteredInCatalog_FixesMainTankIndexFilter()
    {
        var upgrade = (await TenantSchemaUpgradeCatalog.GetCurrentAsync()).Single(x => x.Version == 17);

        Assert.NotNull(upgrade);
        Assert.Equal(17, upgrade.Version);
        Assert.Contains("DROP INDEX IF EXISTS \"IX_FuelTanks_SubStationId_FuelLookupId\"", upgrade.Sql);
        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_FuelTanks_SubStationId_FuelLookupId\"", upgrade.Sql);
        Assert.Contains("\"TankRole\" = 1 AND \"DeletedAt\" IS NULL", upgrade.Sql);
    }

    private sealed class RestrictedScopeFilter : IUserScopeFilterService
    {
        private readonly List<int> _accessible;
        public RestrictedScopeFilter(List<int> accessible) => _accessible = accessible;

        public Task<List<int>> GetAccessibleSubStationIdsAsync(int userId) => Task.FromResult(_accessible);
        public Task<List<int>> GetAccessibleStationIdsAsync(int userId) => Task.FromResult(new List<int> { 1 });
        public Task<List<int>> GetAccessibleBranchIdsAsync(int userId) => Task.FromResult(new List<int> { 1 });
        public Task<List<int>> GetAccessibleMeterIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetUserIdsWithAccessToSubStationAsync(int subStationId) => Task.FromResult(new List<int>());
        public Task<bool> HasAccessToSubStationAsync(int userId, int subStationId) => Task.FromResult(_accessible.Contains(subStationId));
        public Task<bool> HasFullAccessToStationAsync(int userId, int stationId) => Task.FromResult(true);
    }
}
