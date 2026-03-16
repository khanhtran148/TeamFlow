using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.Dtos;

namespace TeamFlow.Application.Features.Dashboard.GetCycleTime;

public sealed class GetCycleTimeHandler(
    IDashboardRepository dashboardRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<GetCycleTimeQuery, Result<CycleTimeDto>>
{
    public async Task<Result<CycleTimeDto>> Handle(GetCycleTimeQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Project_View, ct))
            return Result.Failure<CycleTimeDto>("Access denied");

        var data = await dashboardRepository.GetCycleTimeDataAsync(
            request.ProjectId, request.FromDate, request.ToDate, ct);
        return Result.Success(data);
    }
}
