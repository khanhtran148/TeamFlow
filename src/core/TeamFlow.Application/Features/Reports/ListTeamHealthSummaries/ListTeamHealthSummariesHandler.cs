using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Features.Reports.ListTeamHealthSummaries;

public sealed class ListTeamHealthSummariesHandler(
    ITeamHealthSummaryRepository teamHealthRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<ListTeamHealthSummariesQuery, Result<PagedResult<TeamHealthSummaryDto>>>
{
    public async Task<Result<PagedResult<TeamHealthSummaryDto>>> Handle(
        ListTeamHealthSummariesQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Project_View, ct))
            return Result.Failure<PagedResult<TeamHealthSummaryDto>>("Access denied");

        var (items, totalCount) = await teamHealthRepository.ListByProjectAsync(
            request.ProjectId, request.Page, request.PageSize, ct);

        var dtos = items.Select(s => new TeamHealthSummaryDto(
            s.Id, s.ProjectId, s.PeriodStart, s.PeriodEnd, s.SummaryData, s.GeneratedAt
        )).ToList();

        return Result.Success(new PagedResult<TeamHealthSummaryDto>(dtos, totalCount, request.Page, request.PageSize));
    }
}
