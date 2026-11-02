using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

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
public class FuelTrackHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        await base.OnConnectedAsync();
    }

    public async Task JoinStationGroup(int stationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"station_{stationId}");
    }

    public async Task LeaveStationGroup(int stationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"station_{stationId}");
    }

    public async Task JoinSubStationGroup(int subStationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"substation_{subStationId}");
    }

    public async Task LeaveSubStationGroup(int subStationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"substation_{subStationId}");
    }
}
