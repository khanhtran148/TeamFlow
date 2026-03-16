using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Reports.GetTeamHealthSummary;

public sealed class GetTeamHealthSummaryHandler(
    ITeamHealthSummaryRepository teamHealthRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<GetTeamHealthSummaryQuery, Result<TeamHealthSummaryDto>>
{
    public async Task<Result<TeamHealthSummaryDto>> Handle(GetTeamHealthSummaryQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Project_View, ct))
            return Result.Failure<TeamHealthSummaryDto>("Access denied");

        var summary = await teamHealthRepository.GetLatestByProjectAsync(request.ProjectId, ct);
        if (summary is null)
            return Result.Failure<TeamHealthSummaryDto>("Team health summary not found");

        return Result.Success(new TeamHealthSummaryDto(
            summary.Id, summary.ProjectId, summary.PeriodStart,
            summary.PeriodEnd, summary.SummaryData, summary.GeneratedAt));
    }
}
