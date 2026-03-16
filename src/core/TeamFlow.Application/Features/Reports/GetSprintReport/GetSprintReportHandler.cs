using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Reports.GetSprintReport;

public sealed class GetSprintReportHandler(
    ISprintReportRepository sprintReportRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<GetSprintReportQuery, Result<SprintReportDto>>
{
    public async Task<Result<SprintReportDto>> Handle(GetSprintReportQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Project_View, ct))
            return Result.Failure<SprintReportDto>("Access denied");

        var report = await sprintReportRepository.GetBySprintIdAsync(request.SprintId, ct);
        if (report is null)
            return Result.Failure<SprintReportDto>("Sprint report not found");

        return Result.Success(new SprintReportDto(
            report.Id, report.SprintId, report.ProjectId,
            report.ReportData, report.GeneratedAt, report.GeneratedBy));
    }
}
