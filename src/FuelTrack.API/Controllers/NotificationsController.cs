using FuelTrack.Application.DTOs.Notifications;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _service;
    private readonly IRealtimeNotifier _notifier;

    public NotificationsController(INotificationService service, IRealtimeNotifier notifier)
    {
        _service = service;
        _notifier = notifier;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var result = await _service.GetUserNotificationsAsync(userId);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        var count = await _service.GetUnreadCountAsync(userId);
        return Ok(count);
    }

    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetUserId();
        await _service.MarkAsReadAsync(id, userId);
        await _notifier.NotifyAllAsync("NotificationRead", new { id });
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        await _service.MarkAllAsReadAsync(userId);
        await _notifier.NotifyAllAsync("AllNotificationsRead", new { });
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto)
    {
        var userId = GetUserId();
        var result = await _service.CreateAsync(dto, userId);
        await _notifier.NotifyAllAsync("NotificationCreated", new { id = result.Id, result.Title });
        return CreatedAtAction(nameof(GetAll), null, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        await _notifier.NotifyAllAsync("NotificationDeleted", new { id });
        return NoContent();
    }

}
