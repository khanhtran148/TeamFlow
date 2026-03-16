using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.Invitations;
using TeamFlow.Application.Features.Invitations.Accept;
using TeamFlow.Application.Features.Invitations.Create;
using TeamFlow.Application.Features.Invitations.List;
using TeamFlow.Application.Features.Invitations.ListPendingForUser;
using TeamFlow.Application.Features.Invitations.Revoke;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organizations/{orgId:guid}/invitations")]
public sealed class InvitationsController : ApiControllerBase
{
    /// <summary>POST /organizations/{orgId}/invitations — create invitation (returns raw token once).</summary>
    [HttpPost]
    [EnableRateLimiting("write")]
    [ProducesResponseType(typeof(CreateInvitationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        Guid orgId,
        [FromBody] CreateInvitationRequest request,
        CancellationToken ct)
    {
        var cmd = new CreateInvitationCommand(orgId, request.Email, request.Role);
        var result = await Sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(List), new { orgId }, result.Value)
            : HandleResult(result);
    }

    /// <summary>GET /organizations/{orgId}/invitations — list invitations for org.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InvitationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(Guid orgId, CancellationToken ct)
    {
        var result = await Sender.Send(new ListInvitationsQuery(orgId), ct);
        return HandleResult(result);
    }
}

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/invitations")]
public sealed class InvitationActionsController : ApiControllerBase
{
    /// <summary>POST /invitations/{token}/accept — accept invitation by raw token.</summary>
    [HttpPost("{token}/accept")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(typeof(AcceptInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Accept(string token, CancellationToken ct)
    {
        var result = await Sender.Send(new AcceptInvitationCommand(token), ct);
        return HandleResult(result);
    }

    /// <summary>GET /invitations/pending — list pending invitations for the current user.</summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<InvitationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPending(CancellationToken ct)
    {
        var result = await Sender.Send(new ListPendingForUserQuery(), ct);
        return HandleResult(result);
    }

    /// <summary>DELETE /invitations/{id} — revoke invitation.</summary>
    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new RevokeInvitationCommand(id), ct);
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }
}

public sealed record CreateInvitationRequest(string? Email, OrgRole Role);
