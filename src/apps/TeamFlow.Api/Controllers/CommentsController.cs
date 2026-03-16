using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Api.RateLimiting;
using TeamFlow.Application.Features.Comments;
using TeamFlow.Application.Features.Comments.CreateComment;
using TeamFlow.Application.Features.Comments.DeleteComment;
using TeamFlow.Application.Features.Comments.GetComments;
using TeamFlow.Application.Features.Comments.UpdateComment;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
public sealed class CommentsController : ApiControllerBase
{
    [HttpGet("/api/v{version:apiVersion}/workitems/{workItemId:guid}/comments")]
    [ProducesResponseType(typeof(GetCommentsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByWorkItem(
        Guid workItemId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetCommentsQuery(workItemId, page, pageSize), ct);
        return HandleResult(result);
    }

    [HttpPost("/api/v{version:apiVersion}/workitems/{workItemId:guid}/comments")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        Guid workItemId,
        [FromBody] CreateCommentBody body,
        CancellationToken ct)
    {
        var cmd = new CreateCommentCommand(workItemId, body.Content, body.ParentCommentId);
        var result = await Sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetByWorkItem), new { workItemId }, result.Value)
            : HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCommentBody body, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateCommentCommand(id, body.Content), ct);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new DeleteCommentCommand(id), ct);
        return HandleResult(result);
    }
}

public sealed record CreateCommentBody(string Content, Guid? ParentCommentId);
public sealed record UpdateCommentBody(string Content);
