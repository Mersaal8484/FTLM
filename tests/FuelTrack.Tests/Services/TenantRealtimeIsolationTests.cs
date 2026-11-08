using System.Security.Claims;
using FuelTrack.API.Hubs;
using FuelTrack.API.Services;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Moq;
using Microsoft.Extensions.DependencyInjection;

namespace FuelTrack.Tests.Services;

public sealed class TenantRealtimeIsolationTests
{
    private static IConfiguration Config() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["MultiTenancy:Enabled"] = "true" }).Build();
    private sealed record Tenant(Guid Id) : ITenantRequestContext { public Guid? TenantId => Id; public string? DatabaseConnection => null; }

    [Fact]
    public async Task HubGroups_AreTenantScoped_AndRejectMismatchedClaims()
    {
        var id = Guid.NewGuid();
        var caller = new Mock<HubCallerContext>();
        caller.SetupGet(x => x.ConnectionId).Returns("c");
        caller.SetupGet(x => x.UserIdentifier).Returns("7");
        caller.SetupGet(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "7"), new Claim("tenant_id", id.ToString())], "test")));
        var scopes = new Mock<IUserScopeFilterService>();
        scopes.Setup(x => x.HasAccessToSubStationAsync(7, 3)).ReturnsAsync(true);
        var groups = new Mock<IGroupManager>();
        IServiceProvider Services(Guid tenantId) => new ServiceCollection().AddSingleton(scopes.Object).AddSingleton<ITenantRequestContext>(new Tenant(tenantId)).BuildServiceProvider();
        var hub = new FuelTrackHub(Config(), Services(id)) { Context = caller.Object, Groups = groups.Object };
        await hub.OnConnectedAsync();
        await hub.JoinSubStationGroup(3);
        groups.Verify(x => x.AddToGroupAsync("c", $"tenant:{id:N}:all", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(x => x.AddToGroupAsync("c", $"tenant:{id:N}:group:user_7", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(x => x.AddToGroupAsync("c", $"tenant:{id:N}:group:substation_3", It.IsAny<CancellationToken>()), Times.Once);
        var wrong = new FuelTrackHub(Config(), Services(Guid.NewGuid())) { Context = caller.Object, Groups = groups.Object };
        await Assert.ThrowsAsync<HubException>(() => wrong.JoinSubStationGroup(3));
    }

    [Fact]
    public async Task Broadcast_UsesTenantGroup_AndNeverGlobalClients()
    {
        var id = Guid.NewGuid();
        var proxy = new Mock<IClientProxy>();
        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.Group($"tenant:{id:N}:all")).Returns(proxy.Object);
        var hub = new Mock<IHubContext<FuelTrackHub>>();
        hub.SetupGet(x => x.Clients).Returns(clients.Object);
        await new RealtimeNotifier(hub.Object, Config(), new Tenant(id)).NotifyAllAsync("event", new { });
        clients.Verify(x => x.Group($"tenant:{id:N}:all"), Times.Once);
        clients.VerifyGet(x => x.All, Times.Never);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new RealtimeNotifier(hub.Object, Config()).NotifyAllAsync("event", null));
    }
}
