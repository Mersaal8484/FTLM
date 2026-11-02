using FuelTrack.Application.DTOs.Notifications;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public NotificationService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, ICurrentUserService currentUser, IRealtimeNotifier realtimeNotifier)
    {
        _db = db;
        _scopeFilter = scopeFilter;
        _currentUser = currentUser;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<NotificationsResponseDto> GetUserNotificationsAsync(int userId)
    {
        var query = _db.NotificationRecipients
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Notification.CreatedAt);

        var notifications = await query
            .Select(r => new NotificationDto(
                r.Notification.Id, r.Notification.SubStationId, r.Notification.Title,
                r.Notification.Message, r.Notification.NotificationType, r.Notification.Severity,
                r.IsRead, r.Notification.RelatedEntityType, r.Notification.RelatedEntityId,
                r.Notification.SentAt, r.Notification.SentById, r.Notification.CreatedAt))
            .ToListAsync();

        return new NotificationsResponseDto(notifications, notifications.Count(n => !n.IsRead));
    }

    public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto, int? sentById)
    {
        var requestedRecipientIds = dto.RecipientUserIds?.Distinct().ToList()
            ?? await ResolveRecipientsAsync(dto.SubStationId);
        var recipientIds = await _db.Users
            .Where(u => u.IsActive && requestedRecipientIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        var notification = new Notification
        {
            SubStationId = dto.SubStationId,
            Title = dto.Title,
            Message = dto.Message,
            NotificationType = dto.NotificationType,
            Severity = dto.Severity,
            RelatedEntityType = dto.RelatedEntityType,
            RelatedEntityId = dto.RelatedEntityId,
            IsRead = false,
            SentAt = DateTime.UtcNow,
            SentById = sentById,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Notifications.Add(notification);
        foreach (var recipientId in recipientIds)
        {
            notification.Recipients.Add(new NotificationRecipient
            {
                UserId = recipientId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        return ToDto(notification, false);
    }

    public async Task<NotificationDto?> PublishScopedAsync(
        CreateNotificationDto dto, int? sentById, IReadOnlyCollection<string> moduleKeys, CancellationToken ct = default)
    {
        var recipientIds = await ResolveAuthorizedRecipientsAsync(dto.SubStationId, moduleKeys, ct);
        if (recipientIds.Count == 0) return null;
        var notification = await CreateAsync(dto with { RecipientUserIds = recipientIds }, sentById);
        await Task.WhenAll(recipientIds.Select(userId => _realtimeNotifier.NotifyUserAsync(
            userId.ToString(), "NotificationCreated", notification)));
        return notification;
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var recipient = await _db.NotificationRecipients
            .FirstOrDefaultAsync(r => r.NotificationId == notificationId && r.UserId == userId)
            ?? throw new KeyNotFoundException($"Notification {notificationId} not found for the current user.");
        recipient.IsRead = true;
        recipient.ReadAt = DateTime.UtcNow;
        recipient.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var unread = await _db.NotificationRecipients
            .Where(r => r.UserId == userId && !r.IsRead)
            .ToListAsync();
        foreach (var recipient in unread)
        {
            recipient.IsRead = true;
            recipient.ReadAt = DateTime.UtcNow;
            recipient.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _db.NotificationRecipients.CountAsync(r => r.UserId == userId && !r.IsRead);
    }

    public async Task DeleteAsync(int id)
    {
        var notification = await _db.Notifications.FindAsync(id)
            ?? throw new KeyNotFoundException($"Notification {id} not found.");
        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();
    }

    private async Task<List<int>> ResolveRecipientsAsync(int? subStationId)
    {
        if (subStationId.HasValue)
            return await _scopeFilter.GetUserIdsWithAccessToSubStationAsync(subStationId.Value);

        return await _db.Users
            .Where(u => u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();
    }

    private async Task<List<int>> ResolveAuthorizedRecipientsAsync(int? subStationId, IReadOnlyCollection<string> moduleKeys, CancellationToken ct)
    {
        var scopedUserIds = await ResolveRecipientsAsync(subStationId);
        var privilegedUserIds = await (from userRole in _db.UserRoles
                                       join role in _db.Roles on userRole.RoleId equals role.Id
                                       join user in _db.Users on userRole.UserId equals user.Id
                                       where user.IsActive && (role.Name == "Admin" || role.Name == "SystemOwner")
                                       select user.Id).Distinct().ToListAsync(ct);

        var permittedUserIds = await (from userRole in _db.UserRoles
                                      join user in _db.Users on userRole.UserId equals user.Id
                                      join rolePermission in _db.RolePermissions on userRole.RoleId equals rolePermission.RoleId
                                      join pagePermission in _db.PagePermissions on rolePermission.PagePermissionId equals pagePermission.Id
                                      join module in _db.ModuleDefinitions on pagePermission.ModuleDefinitionId equals module.Id
                                      where user.IsActive && rolePermission.IsGranted && moduleKeys.Contains(module.Key)
                                          && (pagePermission.ActionKey == "view" || pagePermission.ActionKey == "manage")
                                      select user.Id).Distinct().ToListAsync(ct);

        return scopedUserIds.Intersect(permittedUserIds).Union(privilegedUserIds).Distinct().ToList();
    }

    private static NotificationDto ToDto(Notification notification, bool isRead) => new(
        notification.Id, notification.SubStationId, notification.Title,
        notification.Message, notification.NotificationType, notification.Severity,
        isRead, notification.RelatedEntityType, notification.RelatedEntityId,
        notification.SentAt, notification.SentById, notification.CreatedAt);
}
