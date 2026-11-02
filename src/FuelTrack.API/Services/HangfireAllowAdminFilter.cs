using Hangfire.Dashboard;

namespace FuelTrack.API.Services;

public class HangfireAllowAdminFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && (httpContext.User.IsInRole("SystemOwner") || httpContext.User.IsInRole("Admin"));
    }
}
