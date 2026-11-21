using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FuelTrack.Tests.Services;

public sealed class GeneratorMaintenanceOilConfigurationServiceTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly GeneratorMaintenanceOilConfigurationService _service;

    public GeneratorMaintenanceOilConfigurationServiceTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"GeneratorMaintenanceOilConfig_{Guid.NewGuid()}").Options);
        _service = new GeneratorMaintenanceOilConfigurationService(
            _db,
            new StubUserScopeFilterService(),
            new StubCurrentUserService { IsAdmin = true, UserId = 1 });
    }

    [Fact]
    public async Task SaveAsync_UsesOnlyTheOilConfiguredForTheGeneratorAndServiceItem()
    {
        await SeedAsync();

        var result = await _service.SaveAsync(new SaveGeneratorMaintenanceOilConfigurationDto(10, 20, 30, 18.1256m), 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(18.126m, result.Value!.QuantityLiters);
        Assert.Equal(30, result.Value.OilId);

        var rows = await _service.GetAllAsync(10);
        Assert.Single(rows);
        Assert.Equal("Engine oil", rows[0].OilLookupName);
    }

    [Fact]
    public async Task SaveAsync_RejectsOilFromAnotherSubStation()
    {
        await SeedAsync();
        _db.Oils.Add(new Oil { Id = 31, OilLookupId = 3, CurrentStock = 100, MinStock = 10, SubStationId = 2 });
        await _db.SaveChangesAsync();

        var result = await _service.SaveAsync(new SaveGeneratorMaintenanceOilConfigurationDto(10, 20, 31, 12m), 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingGeneratorAndItemConfigurationWithoutCreatingADefault()
    {
        await SeedAsync();
        await _service.SaveAsync(new SaveGeneratorMaintenanceOilConfigurationDto(10, 20, 30, 10m), 1);

        var result = await _service.SaveAsync(new SaveGeneratorMaintenanceOilConfigurationDto(10, 20, 30, 15m), 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(15m, result.Value!.QuantityLiters);
        Assert.Single(_db.GeneratorMaintenanceOilConfigurations);
    }

    private async Task SeedAsync()
    {
        var station = new Station { Id = 1, Name = "Station", Code = "ST" };
        var subStation = new SubStation { Id = 1, StationId = 1, Name = "Technical", Code = "TECH" };
        var lookup = new Lookup { Id = 3, CategoryId = 1, Code = "OIL", Name = "Engine oil" };
        _db.AddRange(station, subStation, lookup,
            new Generator { Id = 10, Name = "G-1", SubStationId = 1 },
            new ServiceItem { Id = 20, Code = "OilChange", NameAr = "تغيير زيت", NameEn = "Oil change", IsActive = true },
            new Oil { Id = 30, OilLookupId = 3, CurrentStock = 100, MinStock = 10, SubStationId = 1 });
        await _db.SaveChangesAsync();
    }

    public void Dispose() => _db.Dispose();
}
