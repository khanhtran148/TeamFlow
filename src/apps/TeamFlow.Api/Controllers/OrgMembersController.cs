using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.OrgMembers;
using TeamFlow.Application.Features.OrgMembers.ChangeRole;
using TeamFlow.Application.Features.OrgMembers.List;
using TeamFlow.Application.Features.OrgMembers.Remove;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organizations/{orgId:guid}/members")]
public sealed class OrgMembersController : ApiControllerBase
{
    /// <summary>GET /organizations/{orgId}/members — list all org members.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrgMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(Guid orgId, CancellationToken ct)
    {
        var result = await Sender.Send(new ListOrgMembersQuery(orgId), ct);
        return HandleResult(result);
    }

    /// <summary>PUT /organizations/{orgId}/members/{userId}/role — change a member's role.</summary>
    [HttpPut("{userId:guid}/role")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRole(
        Guid orgId,
        Guid userId,
        [FromBody] ChangeRoleRequest request,
        CancellationToken ct)
    {
        var cmd = new ChangeOrgMemberRoleCommand(orgId, userId, request.Role);
        var result = await Sender.Send(cmd, ct);
        return HandleResult(result);
    }

    /// <summary>DELETE /organizations/{orgId}/members/{userId} — remove a member.</summary>
    [HttpDelete("{userId:guid}")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid orgId, Guid userId, CancellationToken ct)
    {
        var result = await Sender.Send(new RemoveOrgMemberCommand(orgId, userId), ct);
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }
}

public sealed record ChangeRoleRequest(OrgRole Role);
