using FuelTrack.Application.Interfaces;

namespace FuelTrack.Tests.Services;

public class StubCurrentUserService : ICurrentUserService
{
    public int? UserId { get; set; } = 1;
    public string? UserName { get; set; } = "test@example.com";
    public bool IsAuthenticated { get; set; } = true;
    public bool IsAdmin { get; set; } = true;
    public bool IsSystemOwner { get; set; } = false;
    public bool HasAnyAdminRole => IsAdmin || IsSystemOwner;
}
