using System.Security.Claims;
using FuelTrack.Application.DTOs.Notifications;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly FuelTrackDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserScopeFilterService _scopeFilter;

    public NotificationService(FuelTrackDbContext db, IHttpContextAccessor httpContextAccessor, IUserScopeFilterService scopeFilter)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _scopeFilter = scopeFilter;
    }

    private int? GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private bool IsAdmin()
    {
        return _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(int userId)
    {
        var query = _db.Notifications
            .AsQueryable();

        if (!IsAdmin())
        {
            var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId);
            query = query.Where(n => n.SubStationId.HasValue && accessibleIds.Contains(n.SubStationId.Value));
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(
                n.Id, n.SubStationId, n.Title, n.Message,
                n.NotificationType, n.Severity, n.IsRead,
                n.SentAt, n.SentById, n.CreatedAt))
            .ToListAsync();
    }

    public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto, int sentById)
    {
        var notification = new Notification
        {
            SubStationId = dto.SubStationId,
            Title = dto.Title,
            Message = dto.Message,
            NotificationType = dto.NotificationType,
            Severity = dto.Severity,
            IsRead = false,
            SentAt = DateTime.UtcNow,
            SentById = sentById,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        return new NotificationDto(
            notification.Id, notification.SubStationId, notification.Title,
            notification.Message, notification.NotificationType,
            notification.Severity, notification.IsRead,
            notification.SentAt, notification.SentById, notification.CreatedAt);
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _db.Notifications.FindAsync(notificationId)
            ?? throw new KeyNotFoundException($"Notification {notificationId} not found.");
        notification.IsRead = true;
        notification.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var query = _db.Notifications
            .Where(n => !n.IsRead);

        if (!IsAdmin())
        {
            var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId);
            query = query.Where(n => n.SubStationId.HasValue && accessibleIds.Contains(n.SubStationId.Value));
        }

        await query.ExecuteUpdateAsync(s => s
            .SetProperty(n => n.IsRead, true)
            .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        var query = _db.Notifications
            .Where(n => !n.IsRead);

        if (!IsAdmin())
        {
            var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId);
            query = query.Where(n => n.SubStationId.HasValue && accessibleIds.Contains(n.SubStationId.Value));
        }

        return await query.CountAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var notification = await _db.Notifications.FindAsync(id)
            ?? throw new KeyNotFoundException($"Notification {id} not found.");
        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();
    }
}
