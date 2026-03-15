using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.Sprints;
using TeamFlow.Application.Features.Sprints.AddItem;
using TeamFlow.Application.Features.Sprints.CompleteSprint;
using TeamFlow.Application.Features.Sprints.CreateSprint;
using TeamFlow.Application.Features.Sprints.DeleteSprint;
using TeamFlow.Application.Features.Sprints.GetBurndown;
using TeamFlow.Application.Features.Sprints.GetSprint;
using TeamFlow.Application.Features.Sprints.ListSprints;
using TeamFlow.Application.Features.Sprints.RemoveItem;
using TeamFlow.Application.Features.Sprints.StartSprint;
using TeamFlow.Application.Features.Sprints.UpdateCapacity;
using TeamFlow.Application.Features.Sprints.UpdateSprint;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
public sealed class SprintsController : ApiControllerBase
{
    [HttpPost]
    [EnableRateLimiting("write")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateSprintCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleResult(result);
    }

    [HttpGet]
    [EnableRateLimiting("general")]
    [ProducesResponseType(typeof(ListSprintsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new ListSprintsQuery(projectId, page, pageSize), ct);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [EnableRateLimiting("general")]
    [ProducesResponseType(typeof(SprintDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetSprintQuery(id), ct);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSprintBody body, CancellationToken ct)
    {
        var result = await Sender.Send(
            new UpdateSprintCommand(id, body.Name, body.Goal, body.StartDate, body.EndDate), ct);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new DeleteSprintCommand(id), ct);
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/start")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new StartSprintCommand(id), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/complete")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new CompleteSprintCommand(id), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/items/{workItemId:guid}")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddItem(Guid id, Guid workItemId, CancellationToken ct)
    {
        var result = await Sender.Send(new AddItemToSprintCommand(id, workItemId), ct);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}/items/{workItemId:guid}")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveItem(Guid id, Guid workItemId, CancellationToken ct)
    {
        var result = await Sender.Send(new RemoveItemFromSprintCommand(id, workItemId), ct);
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/capacity")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCapacity(Guid id, [FromBody] UpdateCapacityBody body, CancellationToken ct)
    {
        var entries = body.Capacity
            .Select(c => new CapacityEntry(c.MemberId, c.Points))
            .ToList();
        var result = await Sender.Send(new UpdateCapacityCommand(id, entries), ct);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/burndown")]
    [EnableRateLimiting("general")]
    [ProducesResponseType(typeof(BurndownDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBurndown(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetBurndownQuery(id), ct);
        return HandleResult(result);
    }
}

public sealed record UpdateSprintBody(string Name, string? Goal, DateOnly? StartDate, DateOnly? EndDate);
public sealed record UpdateCapacityBody(IReadOnlyList<CapacityEntryBody> Capacity);
public sealed record CapacityEntryBody(Guid MemberId, int Points);
