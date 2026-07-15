namespace FuelTrack.Application.Interfaces;

public interface IRealtimeNotifier
{
    Task NotifyAllAsync(string method, object? data);
    Task NotifyGroupAsync(string groupName, string method, object? data);
    Task NotifyUserAsync(string userId, string method, object? data);
}
