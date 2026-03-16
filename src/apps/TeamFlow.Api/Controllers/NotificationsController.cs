using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Notifications;
using TeamFlow.Application.Features.Notifications.GetNotifications;
using TeamFlow.Application.Features.Notifications.GetPreferences;
using TeamFlow.Application.Features.Notifications.GetUnreadCount;
using TeamFlow.Application.Features.Notifications.MarkAllAsRead;
using TeamFlow.Application.Features.Notifications.MarkAsRead;
using TeamFlow.Application.Features.Notifications.UpdatePreferences;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
public sealed class NotificationsController : ApiControllerBase
{
    [HttpGet]
    [EnableRateLimiting("general")]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool? isRead,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetNotificationsQuery(isRead, page, pageSize), ct);
        return HandleResult(result);
    }

    [HttpGet("unread-count")]
    [EnableRateLimiting("general")]
    [ProducesResponseType(typeof(UnreadCountDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var result = await Sender.Send(new GetUnreadCountQuery(), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/read")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new MarkAsReadCommand(id), ct);
        return HandleResult(result);
    }

    [HttpPost("read-all")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var result = await Sender.Send(new MarkAllAsReadCommand(), ct);
        return HandleResult(result);
    }

    [HttpGet("preferences")]
    [EnableRateLimiting("general")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationPreferenceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var result = await Sender.Send(new GetPreferencesQuery(), ct);
        return HandleResult(result);
    }

    [HttpPut("preferences")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdatePreferencesBody body, CancellationToken ct)
    {
        var cmd = new UpdatePreferencesCommand(body.Preferences);
        var result = await Sender.Send(cmd, ct);
        return HandleResult(result);
    }
}
