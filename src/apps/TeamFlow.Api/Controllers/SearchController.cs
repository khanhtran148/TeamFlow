using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Search.FullTextSearch;
using TeamFlow.Application.Features.WorkItems;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
[EnableRateLimiting("search")]
public sealed class SearchController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WorkItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
        [FromQuery] Guid projectId,
        [FromQuery] string? q,
        [FromQuery] WorkItemStatus[]? status,
        [FromQuery] Priority[]? priority,
        [FromQuery] WorkItemType[]? type,
        [FromQuery] Guid? assigneeId,
        [FromQuery] Guid? sprintId,
        [FromQuery] Guid? releaseId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new FullTextSearchQuery(
            projectId, q, status, priority, type,
            assigneeId, sprintId, releaseId, fromDate, toDate,
            page, pageSize);

        var result = await Sender.Send(query, ct);
        return HandleResult(result);
    }
}
