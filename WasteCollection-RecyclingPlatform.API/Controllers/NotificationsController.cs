using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WasteCollection_RecyclingPlatform.Services.DTOs;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationResponse>>> GetNotifications([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var notifs = await _notificationService.GetNotificationsAsync(userId, limit, ct);
        return Ok(notifs);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadNotificationCountResponse>> GetUnreadCount(CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var count = await _notificationService.GetUnreadCountAsync(userId, ct);
        return Ok(new UnreadNotificationCountResponse { Count = count });
    }

    [HttpPatch("{id:long}/read")]
    public async Task<ActionResult> MarkRead(long id, CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var success = await _notificationService.MarkReadAsync(userId, id, ct);
        if (!success) return NotFound();

        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<ActionResult> MarkAllRead(CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        await _notificationService.MarkAllReadAsync(userId, ct);
        return NoContent();
    }
}
