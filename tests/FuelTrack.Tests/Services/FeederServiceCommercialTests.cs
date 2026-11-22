using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FuelTrack.Tests.Services;

public sealed class FeederServiceCommercialTests : IDisposable
{
    private readonly FuelTrackDbContext _db;

    public FeederServiceCommercialTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"FeederServiceCommercial_{Guid.NewGuid()}").Options);

        _db.AddRange(
            new Branch { Id = 1, Code = "TECH", Name = "Technical", IsActive = true },
            new Station { Id = 2, BranchId = 1, Code = "PS01", Name = "Commercial", IsActive = true },
            new SubStation { Id = 3, StationId = 2, Code = "PS01", Name = "Commercial", IsActive = true },
            new CustomerFeatureSetting
            {
                Key = CommercialStationModeService.FeatureKey,
                IsEnabled = true,
                Value = CommercialStationModeService.CommercialModeValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateAndList_CommercialMode_ResolveTechnicalScopeServerSide()
    {
        var service = new FeederService(
            _db,
            new StubUserScopeFilterService(),
            new StubCurrentUserService { UserId = 1, IsAdmin = true },
            new CommercialStationModeService(_db));

        var create = await service.CreateAsync(
            new CreateFeederDto(999, "FDR-01", "Feeder 1", 1m, null), 1);

        Assert.True(create.IsSuccess);
        Assert.Equal(3, create.Value!.SubStationId);

        var feeders = await service.GetBySiteAsync(999, 1);

        Assert.Single(feeders);
        Assert.Equal(3, feeders[0].SubStationId);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
