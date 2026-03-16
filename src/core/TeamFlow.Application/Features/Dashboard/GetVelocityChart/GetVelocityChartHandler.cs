using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.Dtos;

namespace TeamFlow.Application.Features.Dashboard.GetVelocityChart;

public sealed class GetVelocityChartHandler(
    IDashboardRepository dashboardRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<GetVelocityChartQuery, Result<VelocityChartDto>>
{
    public async Task<Result<VelocityChartDto>> Handle(GetVelocityChartQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Project_View, ct))
            return Result.Failure<VelocityChartDto>("Access denied");

        var data = await dashboardRepository.GetVelocityDataAsync(request.ProjectId, request.SprintCount, ct);
        return Result.Success(data);
    }
}
