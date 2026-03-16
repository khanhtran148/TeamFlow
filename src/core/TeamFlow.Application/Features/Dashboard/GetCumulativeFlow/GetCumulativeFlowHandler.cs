using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.Dtos;

namespace TeamFlow.Application.Features.Dashboard.GetCumulativeFlow;

public sealed class GetCumulativeFlowHandler(
    IDashboardRepository dashboardRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<GetCumulativeFlowQuery, Result<CumulativeFlowDto>>
{
    public async Task<Result<CumulativeFlowDto>> Handle(GetCumulativeFlowQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Project_View, ct))
            return Result.Failure<CumulativeFlowDto>("Access denied");

        var data = await dashboardRepository.GetCumulativeFlowDataAsync(
            request.ProjectId, request.FromDate, request.ToDate, ct);
        return Result.Success(data);
    }
}
