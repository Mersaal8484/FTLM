namespace FuelTrack.Application.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsSystemOwner { get; }
    bool HasAnyAdminRole { get; }
}
