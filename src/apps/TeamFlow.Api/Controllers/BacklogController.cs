using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.Backlog.GetBacklog;
using TeamFlow.Application.Features.Backlog.ReorderBacklog;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
public sealed class BacklogController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBacklog(
        [FromQuery] Guid projectId,
        [FromQuery] WorkItemStatus? status,
        [FromQuery] Priority? priority,
        [FromQuery] Guid? assigneeId,
        [FromQuery] WorkItemType? type,
        [FromQuery] Guid? sprintId,
        [FromQuery] Guid? releaseId,
        [FromQuery] bool? unscheduled,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(
            new GetBacklogQuery(projectId, status, priority, assigneeId, type, sprintId, releaseId, unscheduled, search, page, pageSize),
            ct);
        return HandleResult(result);
    }

    [HttpPost("reorder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Reorder([FromBody] ReorderBacklogCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return HandleResult(result);
    }
}
