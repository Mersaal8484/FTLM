using FuelTrack.API.Hubs;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FuelTrack.API.Services;

public class RealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<FuelTrackHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly ITenantRequestContext? _tenant;

    public RealtimeNotifier(IHubContext<FuelTrackHub> hubContext, IConfiguration configuration, ITenantRequestContext? tenant = null)
    {
        _hubContext = hubContext;
        _configuration = configuration;
        _tenant = tenant;
    }

    public Task NotifyAllAsync(string method, object? data)
    {
        var id = TenantRealtimeScope.GetTenant(_configuration, _tenant);
        return (id is Guid tenantId ? _hubContext.Clients.Group(TenantRealtimeScope.All(tenantId)) : _hubContext.Clients.All).SendAsync(method, data);
    }

    public Task NotifyGroupAsync(string groupName, string method, object? data)
        => _hubContext.Clients.Group(TenantRealtimeScope.Group(TenantRealtimeScope.GetTenant(_configuration, _tenant), groupName)).SendAsync(method, data);

    public Task NotifyUserAsync(string userId, string method, object? data)
    {
        var id = TenantRealtimeScope.GetTenant(_configuration, _tenant);
        return (id is Guid tenantId ? _hubContext.Clients.Group(TenantRealtimeScope.Group(tenantId, $"user_{userId}")) : _hubContext.Clients.User(userId)).SendAsync(method, data);
    }
}
