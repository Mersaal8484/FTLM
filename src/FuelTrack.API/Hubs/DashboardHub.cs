using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using FuelTrack.Application.Interfaces;
using FuelTrack.API.Services;

namespace FuelTrack.API.Hubs;

[Authorize(AuthenticationSchemes = "SignalR")]
public class DashboardHub(IConfiguration configuration, IServiceProvider services) : Hub
{
    public async Task JoinDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, DashboardGroup());
    }

    public async Task LeaveDashboard()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, DashboardGroup());
    }

    private string DashboardGroup()
    {
        // Each invocation has its own DI scope; the trusted tenant belongs to
        // the HTTP connection scope selected by the resolution middleware.
        var requestServices = Context.GetHttpContext()?.RequestServices ?? services;
        var tenantId = TenantRealtimeScope.GetTenant(configuration, requestServices.GetService<ITenantRequestContext>());
        if (tenantId is Guid id)
        {
            var claims = Context.User?.FindAll("tenant_id").ToArray() ?? [];
            if (claims.Length != 1 || !Guid.TryParse(claims[0].Value, out var claimId) || claimId != id)
                throw new HubException("Access denied.");
        }
        return TenantRealtimeScope.Group(tenantId, "dashboard");
    }
}
