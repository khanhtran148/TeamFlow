using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.Dtos;

namespace TeamFlow.Application.Features.Dashboard.GetWorkloadHeatmap;

public sealed class GetWorkloadHeatmapHandler(
    IDashboardRepository dashboardRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<GetWorkloadHeatmapQuery, Result<WorkloadHeatmapDto>>
{
    public async Task<Result<WorkloadHeatmapDto>> Handle(GetWorkloadHeatmapQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Project_View, ct))
            return Result.Failure<WorkloadHeatmapDto>("Access denied");

        var data = await dashboardRepository.GetWorkloadDataAsync(request.ProjectId, ct);
        return Result.Success(data);
    }
}
