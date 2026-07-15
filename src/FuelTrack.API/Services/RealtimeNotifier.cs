using FuelTrack.API.Hubs;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FuelTrack.API.Services;

public class RealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<FuelTrackHub> _hubContext;

    public RealtimeNotifier(IHubContext<FuelTrackHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyAllAsync(string method, object? data)
        => _hubContext.Clients.All.SendAsync(method, data);

    public Task NotifyGroupAsync(string groupName, string method, object? data)
        => _hubContext.Clients.Group(groupName).SendAsync(method, data);

    public Task NotifyUserAsync(string userId, string method, object? data)
        => _hubContext.Clients.User(userId).SendAsync(method, data);
}
