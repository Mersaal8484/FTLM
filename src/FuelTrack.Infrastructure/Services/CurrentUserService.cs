using System.Security.Claims;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FuelTrack.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }

    public string? UserName => User?.FindFirst(ClaimTypes.Name)?.Value;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public bool IsAdmin => User?.IsInRole("Admin") == true;
    public bool IsSystemOwner => User?.IsInRole("SystemOwner") == true;
    public bool HasAnyAdminRole => IsAdmin || IsSystemOwner;
}
