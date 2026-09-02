using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalLive.Api.Controllers;

[Route("api/notifications")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetMy([FromQuery] int take = 50)
        => Ok(await _service.GetMyAsync(RequireUserId(), take));

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
        => Ok(new { count = await _service.GetUnreadCountAsync(RequireUserId()) });

    [HttpPost("mark-read")]
    public async Task<IActionResult> MarkRead([FromBody] MarkNotificationsReadRequest? request)
        => HandleResult(await _service.MarkReadAsync(RequireUserId(), request?.Ids));
}
