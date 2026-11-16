using FuelTrack.API.Controllers;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FuelTrack.Tests.Controllers;

public sealed class CommercialHierarchyGuardTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly CommercialStationModeService _mode;

    public CommercialHierarchyGuardTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"CommercialHierarchyGuard_{Guid.NewGuid()}").Options);
        _mode = new CommercialStationModeService(_db);
    }

    [Fact]
    public async Task CommercialMode_HidesTechnicalHierarchyEndpoints()
    {
        await _mode.ActivateAsync(new ActivateCommercialStationModeDto("Commercial Station", "CS-01"), 1);
        var notifier = new Mock<IRealtimeNotifier>();

        var branchResult = await new BranchesController(new Mock<IBranchService>().Object, _mode).GetAll();
        var stationResult = await new StationsController(new Mock<IStationService>().Object, notifier.Object, _mode).GetAll();
        var siteResult = await new SubStationsController(new Mock<ISubStationService>().Object, notifier.Object, _mode).GetAll();

        Assert.IsType<NotFoundResult>(branchResult);
        Assert.IsType<NotFoundResult>(stationResult);
        Assert.IsType<NotFoundResult>(siteResult);
    }

    public void Dispose() => _db.Dispose();
}
