using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using FuelTrack.Application.Interfaces;
using FuelTrack.API.Services;

namespace FuelTrack.API.Hubs;

[Authorize(AuthenticationSchemes = "SignalR")]
public class DashboardHub(IConfiguration configuration, ITenantRequestContext? tenant = null) : Hub
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
        var tenantId = TenantRealtimeScope.GetTenant(configuration, tenant);
        if (tenantId is Guid id)
        {
            var claims = Context.User?.FindAll("tenant_id").ToArray() ?? [];
            if (claims.Length != 1 || !Guid.TryParse(claims[0].Value, out var claimId) || claimId != id)
                throw new HubException("Access denied.");
        }
        return TenantRealtimeScope.Group(tenantId, "dashboard");
    }
}
