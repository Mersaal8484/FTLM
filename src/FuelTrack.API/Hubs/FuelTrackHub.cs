using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using FuelTrack.Application.Interfaces;
using System.Security.Claims;
using FuelTrack.API.Services;

namespace FuelTrack.API.Hubs;

// ORPHAN: This hub is mapped in Program.cs but no frontend code connects to it.
// ModbusPollingService calls Clients.Group("station_X").SendAsync(...) but the
// SignalR JS client never calls JoinStationGroup/LeaveStationGroup.
// Consider: (a) wiring frontend to connect, or (b) removing if not needed.
//
// TODO: C-03 — ModbusPollingService calls SendAsync("SendModbusReading", ...) but
// this Hub has NO handler for "SendModbusReading". Data from Modbus is LOST.
// See ENTERPRISE_WORK_PLAN.md#C-03
[Authorize(AuthenticationSchemes = "SignalR")]
public class FuelTrackHub(IUserScopeFilterService scopes, IConfiguration configuration, ITenantRequestContext? tenant = null) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        var tenantId = CurrentTenant();
        if (tenantId is Guid id)
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantRealtimeScope.All(id));
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantRealtimeScope.Group(tenantId, $"user_{userId}"));
        await base.OnConnectedAsync();
    }

    public async Task JoinStationGroup(int stationId)
    {
        var userId = CurrentUserId();
        if (stationId <= 0 || (!IsAdmin() && !await scopes.HasFullAccessToStationAsync(userId, stationId)))
            throw new HubException("Access denied.");
        await Groups.AddToGroupAsync(Context.ConnectionId, TenantRealtimeScope.Group(CurrentTenant(), $"station_{stationId}"));
    }

    public async Task LeaveStationGroup(int stationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, TenantRealtimeScope.Group(CurrentTenant(), $"station_{stationId}"));
    }

    public async Task JoinSubStationGroup(int subStationId)
    {
        var userId = CurrentUserId();
        if (subStationId <= 0 || (!IsAdmin() && !await scopes.HasAccessToSubStationAsync(userId, subStationId)))
            throw new HubException("Access denied.");
        await Groups.AddToGroupAsync(Context.ConnectionId, TenantRealtimeScope.Group(CurrentTenant(), $"substation_{subStationId}"));
    }

    public async Task LeaveSubStationGroup(int subStationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, TenantRealtimeScope.Group(CurrentTenant(), $"substation_{subStationId}"));
    }

    private int CurrentUserId()
    {
        if (Context.User?.Identity?.IsAuthenticated != true ||
            !int.TryParse(Context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) || id <= 0)
            throw new HubException("Access denied.");
        return id;
    }

    private bool IsAdmin() => Context.User?.IsInRole("Admin") == true || Context.User?.IsInRole("SystemOwner") == true;

    private Guid? CurrentTenant()
    {
        var id = TenantRealtimeScope.GetTenant(configuration, tenant);
        if (id is null) return null;
        var claims = Context.User?.FindAll("tenant_id").ToArray() ?? [];
        if (claims.Length != 1 || !Guid.TryParse(claims[0].Value, out var claimId) || claimId != id)
            throw new HubException("Access denied.");
        return id;
    }
}
