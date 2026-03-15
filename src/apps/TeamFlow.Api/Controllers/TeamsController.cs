using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Teams;
using TeamFlow.Application.Features.Teams.AddTeamMember;
using TeamFlow.Application.Features.Teams.ChangeTeamMemberRole;
using TeamFlow.Application.Features.Teams.CreateTeam;
using TeamFlow.Application.Features.Teams.DeleteTeam;
using TeamFlow.Application.Features.Teams.GetTeam;
using TeamFlow.Application.Features.Teams.ListTeams;
using TeamFlow.Application.Features.Teams.RemoveTeamMember;
using TeamFlow.Application.Features.Teams.UpdateTeam;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
public sealed class TeamsController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateTeamCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetTeamQuery(id), ct);
        return HandleResult(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TeamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] Guid orgId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new ListTeamsQuery(orgId, page, pageSize), ct);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateTeamCommand(id, body.Name, body.Description), ct);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new DeleteTeamCommand(id), ct);
        if (result.IsSuccess)
            return NoContent();
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddTeamMemberBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new AddTeamMemberCommand(id, body.UserId, body.Role), ct);
        if (result.IsSuccess)
            return NoContent();
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct)
    {
        var result = await Sender.Send(new RemoveTeamMemberCommand(id, userId), ct);
        if (result.IsSuccess)
            return NoContent();
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/members/{userId:guid}/role")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRole(Guid id, Guid userId, [FromBody] ChangeRoleBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new ChangeTeamMemberRoleCommand(id, userId, body.NewRole), ct);
        return HandleResult(result);
    }
}

public sealed record UpdateTeamBody(string Name, string? Description);
public sealed record AddTeamMemberBody(Guid UserId, ProjectRole Role);
public sealed record ChangeRoleBody(ProjectRole NewRole);
