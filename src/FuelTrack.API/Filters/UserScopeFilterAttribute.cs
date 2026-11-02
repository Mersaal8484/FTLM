using System.Security.Claims;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FuelTrack.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class UserScopeFilterAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _parameterName;

    public UserScopeFilterAttribute(string parameterName = "subStationId")
    {
        _parameterName = parameterName;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            return;

        if (context.HttpContext.User.IsInRole("SystemOwner") ||
            context.HttpContext.User.IsInRole("Admin"))
            return;

        var userIdStr = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        int? paramValue = null;

        if (context.RouteData.Values.TryGetValue(_parameterName, out var routeValue) && routeValue != null)
        {
            if (int.TryParse(routeValue.ToString(), out var id))
                paramValue = id;
        }

        if (!paramValue.HasValue && context.HttpContext.Request.Query.TryGetValue(_parameterName, out var queryValue))
        {
            if (int.TryParse(queryValue.ToString(), out var id))
                paramValue = id;
        }

        if (!paramValue.HasValue)
            return;

        var scopeFilter = context.HttpContext.RequestServices.GetRequiredService<IUserScopeFilterService>();
        bool hasAccess = _parameterName.ToLowerInvariant() switch
        {
            "substationid" or "subStationId" => await scopeFilter.HasAccessToSubStationAsync(userId, paramValue.Value),
            "stationid" or "stationId" => (await scopeFilter.GetAccessibleStationIdsAsync(userId)).Contains(paramValue.Value),
            "branchid" or "branchId" => (await scopeFilter.GetAccessibleBranchIdsAsync(userId)).Contains(paramValue.Value),
            _ => await scopeFilter.HasAccessToSubStationAsync(userId, paramValue.Value)
        };

        if (!hasAccess)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = 403,
                Title = "Forbidden",
                Detail = "You do not have access to this resource.",
                Instance = context.HttpContext.Request.Path
            })
            {
                StatusCode = 403
            };
        }
    }
}
