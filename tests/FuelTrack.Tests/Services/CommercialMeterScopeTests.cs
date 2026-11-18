using FuelTrack.Application.DTOs.Meters;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FuelTrack.Tests.Services;

/// <summary>
/// C2.5: the hidden technical hierarchy is owned by the commercial-mode service,
/// never by a meter form supplied by a browser client.
/// </summary>
public sealed class CommercialMeterScopeTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly MeterService _service;

    public CommercialMeterScopeTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"CommercialMeterScope_{Guid.NewGuid()}").Options);

        var meterTypeCategory = new LookupCategory
        {
            Id = 1,
            Name = "MeterType",
            DisplayName = "Meter types",
            IsActive = true
        };

        _db.AddRange(
            new Branch { Id = 1, Name = "Technical Branch", Code = "TECH-PS01", IsActive = true },
            new Station { Id = 1, BranchId = 1, Name = "Commercial Station", Code = "PS01", IsActive = true },
            new SubStation { Id = 1, StationId = 1, Name = "Commercial Station", Code = "PS01", IsActive = true },
            meterTypeCategory,
            new Lookup
            {
                Id = 1,
                CategoryId = meterTypeCategory.Id,
                Category = meterTypeCategory,
                Code = "ExportEnergy",
                Name = "Feeder Meter",
                IsActive = true
            },
            new CustomerFeatureSetting
            {
                Key = CommercialStationModeService.FeatureKey,
                IsEnabled = true,
                Value = CommercialStationModeService.CommercialModeValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        _db.SaveChanges();

        _service = new MeterService(
            _db,
            new StubUserScopeFilterService(),
            new StubCurrentUserService { UserId = 1, IsAdmin = true },
            new CommercialStationModeService(_db));
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task CreateAsync_UsesCommercialTechnicalSite_AndCreatesTheFeederMapping()
    {
        var result = await _service.CreateAsync(new CreateMeterDto(
            MeterName: "Outgoing feeder",
            SubStationId: 999,
            StationId: 999,
            MeterTypeLookupId: 1,
            SerialNumber: "FDR-001",
            GeneratorId: 123,
            HoursReading: 4,
            EnergyReading: 100,
            IsActive: true));

        var meter = await _db.Meters.FindAsync(result.Id);
        var feeder = await _db.Feeders.SingleAsync();

        Assert.NotNull(meter);
        Assert.Equal(1, meter.SubStationId);
        Assert.Null(meter.GeneratorId);
        Assert.Equal(meter.Id, feeder.MeterId);
        Assert.Equal(1, feeder.SubStationId);
    }
}
