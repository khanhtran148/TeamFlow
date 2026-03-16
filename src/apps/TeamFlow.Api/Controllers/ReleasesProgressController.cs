using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.Dashboard.Dtos;
using TeamFlow.Application.Features.Dashboard.GetReleaseProgress;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
[EnableRateLimiting("general")]
public sealed class ReleasesProgressController : ApiControllerBase
{
    [HttpGet("/api/v{version:apiVersion}/releases/{releaseId:guid}/progress")]
    [ProducesResponseType(typeof(ReleaseProgressDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProgress(
        Guid releaseId, [FromQuery] Guid projectId, CancellationToken ct)
    {
        var result = await Sender.Send(new GetReleaseProgressQuery(releaseId, projectId), ct);
        return HandleResult(result);
    }
}
