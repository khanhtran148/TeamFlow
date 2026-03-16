using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.Admin.ListOrganizations;
using TeamFlow.Application.Features.Admin.ListUsers;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
[Microsoft.AspNetCore.Authorization.Authorize]
[EnableRateLimiting("general")]
public sealed class AdminController : ApiControllerBase
{
    [HttpGet("organizations")]
    [ProducesResponseType(typeof(IEnumerable<AdminOrganizationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListOrganizations(CancellationToken ct)
    {
        var result = await Sender.Send(new AdminListOrganizationsQuery(), ct);
        return HandleResult(result);
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(IEnumerable<AdminUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListUsers(CancellationToken ct)
    {
        var result = await Sender.Send(new AdminListUsersQuery(), ct);
        return HandleResult(result);
    }
}
