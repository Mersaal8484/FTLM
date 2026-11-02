using System.Security.Claims;
using FuelTrack.API.Hubs;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Microsoft.Extensions.Configuration;

namespace FuelTrack.Tests.Services;

public sealed class FuelTrackHubScopeTests
{
    private static (FuelTrackHub Hub, Mock<IUserScopeFilterService> Scopes, Mock<IGroupManager> Groups) CreateHub(bool admin = false)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "7") };
        if (admin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        var caller = new Mock<HubCallerContext>();
        caller.SetupGet(x => x.ConnectionId).Returns("connection");
        caller.SetupGet(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(claims, "test")));
        var scopes = new Mock<IUserScopeFilterService>();
        scopes.Setup(x => x.HasFullAccessToStationAsync(7, It.IsAny<int>())).ReturnsAsync((int _, int id) => id == 2);
        scopes.Setup(x => x.HasAccessToSubStationAsync(7, It.IsAny<int>())).ReturnsAsync((int _, int id) => id == 3);
        var groups = new Mock<IGroupManager>();
        return (new FuelTrackHub(scopes.Object, new ConfigurationBuilder().Build()) { Context = caller.Object, Groups = groups.Object }, scopes, groups);
    }

    [Fact]
    public async Task StationJoin_EnforcesScope()
    {
        var (hub, _, groups) = CreateHub();
        await hub.JoinStationGroup(2);
        await Assert.ThrowsAsync<HubException>(() => hub.JoinStationGroup(99));
        groups.Verify(x => x.AddToGroupAsync("connection", "station_2", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(x => x.AddToGroupAsync("connection", "station_99", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubStationJoin_EnforcesScope()
    {
        var (hub, _, groups) = CreateHub();
        await hub.JoinSubStationGroup(3);
        await Assert.ThrowsAsync<HubException>(() => hub.JoinSubStationGroup(99));
        groups.Verify(x => x.AddToGroupAsync("connection", "substation_3", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(x => x.AddToGroupAsync("connection", "substation_99", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdminCanJoinValidGroups_ButInvalidIdsAreRejected()
    {
        var (hub, _, groups) = CreateHub(admin: true);
        await hub.JoinStationGroup(99);
        await hub.JoinSubStationGroup(99);
        await Assert.ThrowsAsync<HubException>(() => hub.JoinStationGroup(0));
        groups.Verify(x => x.AddToGroupAsync("connection", "station_99", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(x => x.AddToGroupAsync("connection", "substation_99", It.IsAny<CancellationToken>()), Times.Once);
    }
}
