using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.Search;
using TeamFlow.Application.Features.Search.DeleteSavedFilter;
using TeamFlow.Application.Features.Search.ListSavedFilters;
using TeamFlow.Application.Features.Search.SaveFilter;
using TeamFlow.Application.Features.Search.UpdateSavedFilter;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/saved-filters")]
public sealed class SavedFiltersController : ApiControllerBase
{
    [HttpPost]
    [EnableRateLimiting("write")]
    [ProducesResponseType(typeof(SavedFilterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        Guid projectId,
        [FromBody] SaveFilterBody body,
        CancellationToken ct)
    {
        var cmd = new SaveFilterCommand(projectId, body.Name, body.FilterJson, body.IsDefault);
        var result = await Sender.Send(cmd, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(List), new { projectId }, result.Value)
            : HandleResult(result);
    }

    [HttpGet]
    [EnableRateLimiting("general")]
    [ProducesResponseType(typeof(IReadOnlyList<SavedFilterDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid projectId, CancellationToken ct)
    {
        var result = await Sender.Send(new ListSavedFiltersQuery(projectId), ct);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(typeof(SavedFilterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid projectId,
        Guid id,
        [FromBody] UpdateSavedFilterBody body,
        CancellationToken ct)
    {
        var cmd = new UpdateSavedFilterCommand(projectId, id, body.Name, body.FilterJson, body.IsDefault);
        var result = await Sender.Send(cmd, ct);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid projectId, Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new DeleteSavedFilterCommand(projectId, id), ct);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }
}

public sealed record SaveFilterBody(string Name, JsonDocument FilterJson, bool IsDefault);
public sealed record UpdateSavedFilterBody(string? Name, JsonDocument? FilterJson, bool? IsDefault);
