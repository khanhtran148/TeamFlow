using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.ProjectMemberships;
using TeamFlow.Application.Features.ProjectMemberships.AddProjectMember;
using TeamFlow.Application.Features.ProjectMemberships.ListProjectMemberships;
using TeamFlow.Application.Features.ProjectMemberships.GetMyPermissions;
using TeamFlow.Application.Features.ProjectMemberships.RemoveProjectMember;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/memberships")]
public sealed class ProjectMembershipsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectMembershipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid projectId, CancellationToken ct)
    {
        var result = await Sender.Send(new ListProjectMembershipsQuery(projectId), ct);
        return HandleResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProjectMembershipDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add(
        Guid projectId, [FromBody] AddProjectMemberBody body, CancellationToken ct)
    {
        var result = await Sender.Send(
            new AddProjectMemberCommand(projectId, body.MemberId, body.MemberType, body.Role), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(List), new { projectId }, result.Value)
            : HandleResult(result);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(MyPermissionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPermissions(Guid projectId, CancellationToken ct)
    {
        var result = await Sender.Send(new GetMyPermissionsQuery(projectId), ct);
        return HandleResult(result);
    }

    [HttpDelete("{membershipId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid projectId, Guid membershipId, CancellationToken ct)
    {
        var result = await Sender.Send(new RemoveProjectMemberCommand(membershipId), ct);
        if (result.IsSuccess)
            return NoContent();
        return HandleResult(result);
    }
}

public sealed record AddProjectMemberBody(Guid MemberId, string MemberType, ProjectRole Role);
