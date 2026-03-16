using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Reports;
using TeamFlow.Application.Features.Reports.GetSprintReport;
using TeamFlow.Application.Features.Reports.GetTeamHealthSummary;
using TeamFlow.Application.Features.Reports.ListSprintReports;
using TeamFlow.Application.Features.Reports.ListTeamHealthSummaries;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
[EnableRateLimiting("general")]
public sealed class ReportsController : ApiControllerBase
{
    [HttpGet("/api/v{version:apiVersion}/sprints/{sprintId:guid}/report")]
    [ProducesResponseType(typeof(SprintReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSprintReport(
        Guid sprintId, [FromQuery] Guid projectId, CancellationToken ct)
    {
        var result = await Sender.Send(new GetSprintReportQuery(sprintId, projectId), ct);
        return HandleResult(result);
    }

    [HttpGet("/api/v{version:apiVersion}/projects/{projectId:guid}/reports/sprints")]
    [ProducesResponseType(typeof(PagedResult<SprintReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSprintReports(
        Guid projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new ListSprintReportsQuery(projectId, page, pageSize), ct);
        return HandleResult(result);
    }

    [HttpGet("/api/v{version:apiVersion}/projects/{projectId:guid}/reports/team-health/latest")]
    [ProducesResponseType(typeof(TeamHealthSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestTeamHealth(Guid projectId, CancellationToken ct)
    {
        var result = await Sender.Send(new GetTeamHealthSummaryQuery(projectId), ct);
        return HandleResult(result);
    }

    [HttpGet("/api/v{version:apiVersion}/projects/{projectId:guid}/reports/team-health")]
    [ProducesResponseType(typeof(PagedResult<TeamHealthSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTeamHealthSummaries(
        Guid projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new ListTeamHealthSummariesQuery(projectId, page, pageSize), ct);
        return HandleResult(result);
    }
}
