using FuelTrack.Application.DTOs.Notifications;

namespace FuelTrack.Application.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync(int userId);
    Task<NotificationDto> CreateAsync(CreateNotificationDto dto, int sentById);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task DeleteAsync(int id);
}
