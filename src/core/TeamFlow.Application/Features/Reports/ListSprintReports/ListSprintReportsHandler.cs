using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Features.Reports.ListSprintReports;

public sealed class ListSprintReportsHandler(
    ISprintReportRepository sprintReportRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<ListSprintReportsQuery, Result<PagedResult<SprintReportDto>>>
{
    public async Task<Result<PagedResult<SprintReportDto>>> Handle(
        ListSprintReportsQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Project_View, ct))
            return Result.Failure<PagedResult<SprintReportDto>>("Access denied");

        var (items, totalCount) = await sprintReportRepository.ListByProjectAsync(
            request.ProjectId, request.Page, request.PageSize, ct);

        var dtos = items.Select(r => new SprintReportDto(
            r.Id, r.SprintId, r.ProjectId, r.ReportData, r.GeneratedAt, r.GeneratedBy
        )).ToList();

        return Result.Success(new PagedResult<SprintReportDto>(dtos, totalCount, request.Page, request.PageSize));
    }
}
