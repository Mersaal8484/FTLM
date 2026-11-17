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
/// Marked UNVERIFIED until manually executed by owner.
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
            NominalPowerUnitLookupName: null,
            ActualPowerKW: 1400.0,
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
            NominalPowerUnitLookupName: null,
            ActualPowerKW: 1200.0,
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
    public void Validator_CreateGeneratorDto_AcceptsNull_RejectsNegativeValues()
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
            NominalPowerUnitLookupName: null,
            ActualPowerKW: null,
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
        var nullResult = validator.Validate(validNullDto);
        Assert.True(nullResult.IsValid);

        // Negative value is rejected
        var invalidNegativeDto = validNullDto with { NominalPower = -50.0 };
        var negativeResult = validator.Validate(invalidNegativeDto);
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

        var paged = await scopedService.GetPagedAsync(new GeneratorFilterDto(SubStationId: 99));
        // Generator in substation 99 must not be returned
        Assert.Empty(paged.Data);
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
