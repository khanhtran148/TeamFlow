using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.Dashboard.Dtos;
using TeamFlow.Application.Features.Dashboard.GetCumulativeFlow;
using TeamFlow.Application.Features.Dashboard.GetCycleTime;
using TeamFlow.Application.Features.Dashboard.GetDashboardSummary;
using TeamFlow.Application.Features.Dashboard.GetVelocityChart;
using TeamFlow.Application.Features.Dashboard.GetWorkloadHeatmap;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
[EnableRateLimiting("general")]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(Guid projectId, CancellationToken ct)
    {
        var result = await Sender.Send(new GetDashboardSummaryQuery(projectId), ct);
        return HandleResult(result);
    }

    [HttpGet("velocity")]
    [ProducesResponseType(typeof(VelocityChartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVelocity(
        Guid projectId, [FromQuery] int sprintCount = 10, CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetVelocityChartQuery(projectId, sprintCount), ct);
        return HandleResult(result);
    }

    [HttpGet("cumulative-flow")]
    [ProducesResponseType(typeof(CumulativeFlowDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCumulativeFlow(
        Guid projectId,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        CancellationToken ct)
    {
        var result = await Sender.Send(new GetCumulativeFlowQuery(projectId, fromDate, toDate), ct);
        return HandleResult(result);
    }

    [HttpGet("cycle-time")]
    [ProducesResponseType(typeof(CycleTimeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCycleTime(
        Guid projectId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken ct)
    {
        var result = await Sender.Send(new GetCycleTimeQuery(projectId, fromDate, toDate), ct);
        return HandleResult(result);
    }

    [HttpGet("workload")]
    [ProducesResponseType(typeof(WorkloadHeatmapDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkload(Guid projectId, CancellationToken ct)
    {
        var result = await Sender.Send(new GetWorkloadHeatmapQuery(projectId), ct);
        return HandleResult(result);
    }
}
