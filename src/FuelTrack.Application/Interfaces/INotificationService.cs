using FuelTrack.Application.DTOs.Notifications;

namespace FuelTrack.Application.Interfaces;

public interface INotificationService
{
    Task<NotificationsResponseDto> GetUserNotificationsAsync(int userId);
    Task<NotificationDto> CreateAsync(CreateNotificationDto dto, int? sentById);
    /// <summary>Creates an in-app notification for users who are both in scope and authorised for one of the specified modules.</summary>
    Task<NotificationDto?> PublishScopedAsync(CreateNotificationDto dto, int? sentById, IReadOnlyCollection<string> moduleKeys, CancellationToken ct = default);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task DeleteAsync(int id);
}
