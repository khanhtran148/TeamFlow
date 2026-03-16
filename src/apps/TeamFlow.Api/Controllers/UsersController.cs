using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Users;
using TeamFlow.Application.Features.Users.GetActivityLog;
using TeamFlow.Application.Features.Users.GetCurrentUser;
using TeamFlow.Application.Features.Users.GetProfile;
using TeamFlow.Application.Features.Users.UpdateProfile;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
[EnableRateLimiting("general")]
public sealed class UsersController : ApiControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var result = await Sender.Send(new GetCurrentUserQuery(), ct);
        return HandleResult(result);
    }

    [HttpGet("me/profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await Sender.Send(new GetProfileQuery(), ct);
        return HandleResult(result);
    }

    [HttpPut("me/profile")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return HandleResult(result);
    }

    [HttpGet("me/activity")]
    [ProducesResponseType(typeof(PagedResult<ActivityLogItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetActivityLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetActivityLogQuery(page, pageSize), ct);
        return HandleResult(result);
    }
}
