namespace FuelTrack.Application.DTOs.Notifications;

public record NotificationDto(
    int Id,
    int? SubStationId,
    string Title,
    string Message,
    string NotificationType,
    string? Severity,
    bool IsRead,
    string? RelatedEntityType,
    int? RelatedEntityId,
    DateTime? SentAt,
    int? SentById,
    DateTime CreatedAt);

public record MarkNotificationReadDto(int NotificationId);

public record CreateNotificationDto(
    int? SubStationId,
    string Title,
    string Message,
    string NotificationType,
    string? Severity,
    List<int>? RecipientUserIds,
    string? RelatedEntityType = null,
    int? RelatedEntityId = null);

public record NotificationsResponseDto(List<NotificationDto> Notifications, int UnreadCount);
