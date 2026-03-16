using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.Users;
using TeamFlow.Application.Features.Users.GetCurrentUser;

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
}
