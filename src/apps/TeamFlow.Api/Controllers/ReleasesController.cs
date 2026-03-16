using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.Releases;
using TeamFlow.Application.Features.Releases.AssignItem;
using TeamFlow.Application.Features.Releases.CreateRelease;
using TeamFlow.Application.Features.Releases.DeleteRelease;
using TeamFlow.Application.Features.Releases.GetRelease;
using TeamFlow.Application.Features.Releases.ListReleases;
using TeamFlow.Application.Features.Releases.GetReleaseDetail;
using TeamFlow.Application.Features.Releases.ShipRelease;
using TeamFlow.Application.Features.Releases.UnassignItem;
using TeamFlow.Application.Features.Releases.UpdateRelease;
using TeamFlow.Application.Features.Releases.UpdateReleaseNotes;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
public sealed class ReleasesController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ReleaseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateReleaseCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReleaseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetReleaseQuery(id), ct);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new ListReleasesQuery(projectId, page, pageSize), ct);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReleaseBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateReleaseCommand(id, body.Name, body.Description, body.ReleaseDate), ct);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new DeleteReleaseCommand(id), ct);
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/items/{workItemId:guid}")]
    public async Task<IActionResult> AssignItem(Guid id, Guid workItemId, CancellationToken ct)
    {
        var result = await Sender.Send(new AssignItemToReleaseCommand(id, workItemId), ct);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}/items/{workItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnassignItem(Guid id, Guid workItemId, CancellationToken ct)
    {
        var result = await Sender.Send(new UnassignItemFromReleaseCommand(id, workItemId), ct);
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }
    [HttpGet("{id:guid}/detail")]
    [ProducesResponseType(typeof(ReleaseDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetReleaseDetailQuery(id), ct);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/notes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateNotes(Guid id, [FromBody] UpdateNotesBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateReleaseNotesCommand(id, body.Notes), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/ship")]
    [ProducesResponseType(typeof(ShipReleaseResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Ship(Guid id, [FromBody] ShipBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new ShipReleaseCommand(id, body.ConfirmOpenItems), ct);
        return HandleResult(result);
    }
}

public sealed record UpdateReleaseBody(string Name, string? Description, DateOnly? ReleaseDate);
public sealed record UpdateNotesBody(string Notes);
public sealed record ShipBody(bool ConfirmOpenItems = false);
