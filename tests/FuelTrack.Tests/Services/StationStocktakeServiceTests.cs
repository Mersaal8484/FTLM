using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FuelTrack.Tests.Services;

public sealed class StationStocktakeServiceTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly Mock<IUserScopeFilterService> _scope = new();
    private readonly Mock<ICommercialStationModeService> _commercial = new();
    private readonly Mock<IIdempotencyService> _idempotency = new();
    private readonly Mock<IAlertGeneratorService> _alerts = new();

    public StationStocktakeServiceTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"StationStocktake_{Guid.NewGuid()}").Options);
        _commercial.Setup(x => x.IsCommercialModeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _commercial.Setup(x => x.GetScopeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new CommercialStationScope(1, 2, 3));
        _scope.Setup(x => x.HasAccessToSubStationAsync(It.IsAny<int>(), 3)).ReturnsAsync(true);
    }

    [Fact]
    public async Task RejectAsync_RequiresReason()
    {
        var service = CreateService();
        var result = await service.RejectAsync(123, "", 2);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_RequiresAtLeastOnePhoto()
    {
        var service = CreateService();
        var result = await service.CreateAsync(new(
            new DateOnly(2026, 11, 21), null, Guid.NewGuid(),
            [new(1, 10m, null)], [], []), 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ApproveAsync_RejectsSubmitterAndSecondReview()
    {
        _db.StationStocktakes.Add(new StationStocktake
        {
            Id = 11, StationId = 2, SubStationId = 3, StocktakeDate = new DateOnly(2026, 11, 21),
            Status = StocktakeStatus.Pending, SubmittedByUserId = 1, SubmittedAt = DateTime.UtcNow,
            ClientRequestId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        var service = CreateService();

        var ownReview = await service.ApproveAsync(11, 1);
        var approved = await service.ApproveAsync(11, 2);
        var secondReview = await service.ApproveAsync(11, 3);

        Assert.False(ownReview.IsSuccess);
        Assert.Equal(403, ownReview.StatusCode);
        Assert.True(approved.IsSuccess);
        Assert.False(secondReview.IsSuccess);
        Assert.Equal(409, secondReview.StatusCode);
    }

    private StationStocktakeService CreateService() => new(_db, _commercial.Object, _scope.Object, _idempotency.Object, _alerts.Object);

    public void Dispose() => _db.Dispose();
}
