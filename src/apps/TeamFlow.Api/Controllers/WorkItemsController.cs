using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.WorkItems;
using TeamFlow.Application.Features.WorkItems.AddLink;
using TeamFlow.Application.Features.WorkItems.AssignWorkItem;
using TeamFlow.Application.Features.WorkItems.ChangeStatus;
using TeamFlow.Application.Features.WorkItems.CheckBlockers;
using TeamFlow.Application.Features.WorkItems.CreateWorkItem;
using TeamFlow.Application.Features.WorkItems.DeleteWorkItem;
using TeamFlow.Application.Features.WorkItems.GetLinks;
using TeamFlow.Application.Features.WorkItems.GetWorkItem;
using TeamFlow.Application.Features.WorkItems.MoveWorkItem;
using TeamFlow.Application.Features.WorkItems.RemoveLink;
using TeamFlow.Application.Features.WorkItems.GetHistory;
using TeamFlow.Application.Features.WorkItems.UnassignWorkItem;
using TeamFlow.Application.Features.WorkItems.UpdateWorkItem;
using TeamFlow.Application.Features.Backlog.MarkReadyForSprint;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
public sealed class WorkItemsController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateWorkItemCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetWorkItemQuery(id), ct);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkItemBody body, CancellationToken ct)
    {
        var result = await Sender.Send(
            new UpdateWorkItemCommand(id, body.Title, body.Description, body.Priority, body.EstimationValue, body.AcceptanceCriteria), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new ChangeWorkItemStatusCommand(id, body.Status), ct);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new DeleteWorkItemCommand(id), ct);
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/move")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveWorkItemBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new MoveWorkItemCommand(id, body.NewParentId), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new AssignWorkItemCommand(id, body.AssigneeId), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/unassign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Unassign(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new UnassignWorkItemCommand(id), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/links")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddLink(Guid id, [FromBody] AddLinkBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new AddWorkItemLinkCommand(id, body.TargetId, body.LinkType), ct);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}/links/{linkId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveLink(Guid id, Guid linkId, CancellationToken ct)
    {
        var result = await Sender.Send(new RemoveWorkItemLinkCommand(linkId), ct);
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/links")]
    [ProducesResponseType(typeof(WorkItemLinksDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLinks(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetWorkItemLinksQuery(id), ct);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetWorkItemHistoryQuery(id, page, pageSize), ct);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/blockers")]
    [ProducesResponseType(typeof(BlockersDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBlockers(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new CheckBlockersQuery(id), ct);
        return HandleResult(result);
    }
    [HttpPost("{id:guid}/ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleReady(Guid id, [FromBody] ToggleReadyBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new MarkReadyForSprintCommand(id, body.IsReady), ct);
        return HandleResult(result);
    }
}

public sealed record ToggleReadyBody(bool IsReady);
public sealed record UpdateWorkItemBody(string Title, string? Description, Priority? Priority, decimal? EstimationValue, string? AcceptanceCriteria);
public sealed record ChangeStatusBody(WorkItemStatus Status);
public sealed record MoveWorkItemBody(Guid? NewParentId);
public sealed record AssignBody(Guid AssigneeId);
public sealed record AddLinkBody(Guid TargetId, LinkType LinkType);
