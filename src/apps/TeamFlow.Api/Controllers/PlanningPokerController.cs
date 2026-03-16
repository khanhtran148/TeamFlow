using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.PlanningPoker;
using TeamFlow.Application.Features.PlanningPoker.CastPokerVote;
using TeamFlow.Application.Features.PlanningPoker.ConfirmPokerEstimate;
using TeamFlow.Application.Features.PlanningPoker.CreatePokerSession;
using TeamFlow.Application.Features.PlanningPoker.GetPokerSession;
using TeamFlow.Application.Features.PlanningPoker.RevealPokerVotes;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/poker")]
public sealed class PlanningPokerController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(PokerSessionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePokerSessionCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PokerSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetPokerSessionQuery(id, null), ct);
        return HandleResult(result);
    }

    [HttpGet("by-workitem/{workItemId:guid}")]
    [ProducesResponseType(typeof(PokerSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByWorkItem(Guid workItemId, CancellationToken ct)
    {
        var result = await Sender.Send(new GetPokerSessionQuery(null, workItemId), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/vote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Vote(Guid id, [FromBody] VotePokerBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new CastPokerVoteCommand(id, body.Value), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/reveal")]
    [ProducesResponseType(typeof(PokerSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reveal(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new RevealPokerVotesCommand(id), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(PokerSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Confirm(Guid id, [FromBody] ConfirmPokerBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new ConfirmPokerEstimateCommand(id, body.FinalEstimate), ct);
        return HandleResult(result);
    }
}

public sealed record VotePokerBody(decimal Value);
public sealed record ConfirmPokerBody(decimal FinalEstimate);
