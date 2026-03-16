using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.Retros;
using TeamFlow.Application.Features.Retros.CastRetroVote;
using TeamFlow.Application.Features.Retros.CloseRetroSession;
using TeamFlow.Application.Features.Retros.CreateRetroActionItem;
using TeamFlow.Application.Features.Retros.CreateRetroSession;
using TeamFlow.Application.Features.Retros.GetPreviousActionItems;
using TeamFlow.Application.Features.Retros.GetRetroSession;
using TeamFlow.Application.Features.Retros.ListRetroSessions;
using TeamFlow.Application.Features.Retros.MarkCardDiscussed;
using TeamFlow.Application.Features.Retros.StartRetroSession;
using TeamFlow.Application.Features.Retros.SubmitRetroCard;
using TeamFlow.Application.Features.Retros.TransitionRetroSession;
using TeamFlow.Application.Features.Retros.RenameRetroSession;
using TeamFlow.Application.Features.Retros.DeleteRetroSession;
using TeamFlow.Application.Features.Retros.UpdateColumnsConfig;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
public sealed class RetrosController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(RetroSessionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateRetroSessionCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RetroSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetRetroSessionQuery(id), ct);
        return HandleResult(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListRetroSessionsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new ListRetroSessionsQuery(projectId, page, pageSize), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(typeof(RetroSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new StartRetroSessionCommand(id), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/transition")]
    [ProducesResponseType(typeof(RetroSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Transition(
        Guid id, [FromBody] TransitionBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new TransitionRetroSessionCommand(id, body.TargetStatus), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(RetroSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new CloseRetroSessionCommand(id), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/cards")]
    [ProducesResponseType(typeof(RetroCardDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitCard(Guid id, [FromBody] SubmitCardBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new SubmitRetroCardCommand(id, body.Category, body.Content), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id }, result.Value)
            : HandleResult(result);
    }

    [HttpPost("{id:guid}/cards/{cardId:guid}/vote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Vote(Guid id, Guid cardId, [FromBody] VoteBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new CastRetroVoteCommand(cardId, body.VoteCount), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/cards/{cardId:guid}/discussed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkDiscussed(Guid id, Guid cardId, CancellationToken ct)
    {
        var result = await Sender.Send(new MarkCardDiscussedCommand(cardId), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/action-items")]
    [ProducesResponseType(typeof(RetroActionItemDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateActionItem(
        Guid id, [FromBody] CreateRetroActionItemCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd with { SessionId = id }, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id }, result.Value)
            : HandleResult(result);
    }

    [HttpPut("{id:guid}/name")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rename(
        Guid id, [FromBody] RenameRetroBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new RenameRetroSessionCommand(id, body.Name), ct);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new DeleteRetroSessionCommand(id), ct);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    [HttpPut("{id:guid}/columns-config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateColumnsConfig(
        Guid id, [FromBody] UpdateColumnsConfigBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateColumnsConfigCommand(id, body.ColumnsConfig), ct);
        return HandleResult(result);
    }

    [HttpGet("previous-actions")]
    [ProducesResponseType(typeof(IReadOnlyList<RetroActionItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreviousActions(
        [FromQuery] Guid projectId, CancellationToken ct)
    {
        var result = await Sender.Send(new GetPreviousActionItemsQuery(projectId), ct);
        return HandleResult(result);
    }
}

public sealed record TransitionBody(Domain.Enums.RetroSessionStatus TargetStatus);
public sealed record SubmitCardBody(Domain.Enums.RetroCardCategory Category, string Content);
public sealed record VoteBody(short VoteCount = 1);
public sealed record RenameRetroBody(string Name);
public sealed record UpdateColumnsConfigBody(System.Text.Json.JsonDocument ColumnsConfig);
