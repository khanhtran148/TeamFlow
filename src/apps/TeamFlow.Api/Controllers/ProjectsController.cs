using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.Projects;
using TeamFlow.Application.Features.Projects.ArchiveProject;
using TeamFlow.Application.Features.Projects.CreateProject;
using TeamFlow.Application.Features.Projects.DeleteProject;
using TeamFlow.Application.Features.Projects.GetProject;
using TeamFlow.Application.Features.Projects.ListProjects;
using TeamFlow.Application.Features.Projects.UpdateProject;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
public sealed class ProjectsController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProjectCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetProjectQuery(id), ct);
        return HandleResult(result);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? orgId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new ListProjectsQuery(orgId, status, search, page, pageSize), ct);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateProjectCommand(id, body.Name, body.Description), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new ArchiveProjectCommand(id), ct);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new DeleteProjectCommand(id), ct);
        if (result.IsSuccess)
            return NoContent();
        return HandleResult(result);
    }
}

public sealed record UpdateProjectBody(string Name, string? Description);
