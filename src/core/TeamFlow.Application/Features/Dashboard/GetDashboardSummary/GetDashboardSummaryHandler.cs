using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.Dtos;

namespace TeamFlow.Application.Features.Dashboard.GetDashboardSummary;

public sealed class GetDashboardSummaryHandler(
    IDashboardRepository dashboardRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Project_View, ct))
            return Result.Failure<DashboardSummaryDto>("Access denied");

        var data = await dashboardRepository.GetDashboardSummaryAsync(request.ProjectId, ct);
        return Result.Success(data);
    }
}
