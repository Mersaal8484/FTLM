using FuelTrack.API.Jobs;
using FuelTrack.Application.Common;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FuelTrack.Tests.Jobs;

public class FuelStockAlertJobTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly Mock<IEmailService> _emailMock;
    private readonly Mock<IUserScopeFilterService> _scopeFilterMock;
    private readonly FuelStockAlertJob _job;

    public FuelStockAlertJobTests()
    {
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"FuelStockAlertTestDb_{Guid.NewGuid()}")
            .Options;
        _db = new FuelTrackDbContext(options);

        _emailMock = new Mock<IEmailService>();
        _scopeFilterMock = new Mock<IUserScopeFilterService>();
        var logger = NullLogger<FuelStockAlertJob>.Instance;

        _job = new FuelStockAlertJob(_db, _emailMock.Object, _scopeFilterMock.Object, logger);
    }

    [Fact]
    public async Task ExecuteAsync_NoLowTanks_NoEmailsSent()
    {
        await _job.ExecuteAsync();

        _emailMock.Verify(e => e.SendFuelStockAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FuelStockAlertEmailData>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_LowTanks_NotifiesAuthorizedUsersOnly()
    {
        var subStationId = 1;
        _db.SubStations.Add(new SubStation { Id = subStationId, Name = "SS1", Code = "SS1", StationId = 1 });
        _db.Lookups.Add(new FuelTrack.Domain.Models.Lookup { Id = 1, Name = "Diesel", Code = "Diesel", Category = new LookupCategory { Id = 1, Name = "Fuel" } });
        var tank = new FuelTank
        {
            SubStationId = subStationId,
            Name = "Tank1",
            ActualStockLiters = 50,
            MinAlertLiters = 100,
            CapacityLiters = 500,
            FuelLookupId = 1
        };
        _db.FuelTanks.Add(tank);

        var authorizedUserId = 1;
        _scopeFilterMock.Setup(s => s.GetUserIdsWithAccessToSubStationAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<int> { authorizedUserId });

        var user = new User
        {
            Id = authorizedUserId,
            Email = "user@test.com",
            NormalizedEmail = "USER@TEST.COM",
            NormalizedUserName = "TESTUSER",
            UserName = "testuser",
            FullName = "Test User",
            IsActive = true,
            EmailConfirmed = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var recipientCount = await _db.Users.CountAsync();
        var lowTankCount = await _db.FuelTanks.CountAsync();
        Assert.Equal(1, recipientCount);
        Assert.Equal(1, lowTankCount);

        await _job.ExecuteAsync();

        _emailMock.Verify(e => e.SendFuelStockAlertAsync("user@test.com", "Test User", It.IsAny<FuelStockAlertEmailData>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_LowTanks_NoAuthorizedUsers_NoEmailsSent()
    {
        var subStationId = 1;
        var tank = new FuelTank
        {
            SubStationId = subStationId,
            Name = "Tank1",
            ActualStockLiters = 50,
            MinAlertLiters = 100,
            CapacityLiters = 500
        };
        _db.FuelTanks.Add(tank);

        _scopeFilterMock.Setup(s => s.GetUserIdsWithAccessToSubStationAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<int>());

        await _db.SaveChangesAsync();

        await _job.ExecuteAsync();

        _emailMock.Verify(e => e.SendFuelStockAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FuelStockAlertEmailData>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_LowTanks_ComparesUsableStockAfterDeadStock()
    {
        var subStationId = 1;
        _db.SubStations.Add(new SubStation { Id = subStationId, Name = "SS1", Code = "SS1", StationId = 1 });
        _db.Lookups.Add(new FuelTrack.Domain.Models.Lookup { Id = 2, Name = "Diesel", Code = "Diesel2", Category = new LookupCategory { Id = 2, Name = "Fuel" } });
        var tank = new FuelTank
        {
            SubStationId = subStationId,
            Name = "TankWithDeadStock",
            ActualStockLiters = 120,
            DeadStockLiters = 40,
            MinAlertLiters = 100,
            CapacityLiters = 500,
            FuelLookupId = 2
        };
        _db.FuelTanks.Add(tank);

        var authorizedUserId = 1;
        _scopeFilterMock.Setup(s => s.GetUserIdsWithAccessToSubStationAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<int> { authorizedUserId });

        var user = new User
        {
            Id = authorizedUserId,
            Email = "user2@test.com",
            NormalizedEmail = "USER2@TEST.COM",
            NormalizedUserName = "TESTUSER2",
            UserName = "testuser2",
            FullName = "Test User 2",
            IsActive = true,
            EmailConfirmed = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _job.ExecuteAsync();

        _emailMock.Verify(e => e.SendFuelStockAlertAsync("user2@test.com", "Test User 2", It.IsAny<FuelStockAlertEmailData>()), Times.Once);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
