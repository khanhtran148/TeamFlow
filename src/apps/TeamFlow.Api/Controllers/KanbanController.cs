using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.Kanban.GetKanbanBoard;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
public sealed class KanbanController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(KanbanBoardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBoard(
        [FromQuery] Guid projectId,
        [FromQuery] Guid? assigneeId,
        [FromQuery] WorkItemType? type,
        [FromQuery] Priority? priority,
        [FromQuery] Guid? sprintId,
        [FromQuery] Guid? releaseId,
        [FromQuery] string? swimlane,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(
            new GetKanbanBoardQuery(projectId, assigneeId, type, priority, sprintId, releaseId, swimlane),
            ct);
        return HandleResult(result);
    }
}
