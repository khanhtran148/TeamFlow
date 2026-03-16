using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.Dtos;

namespace TeamFlow.Application.Features.Dashboard.GetReleaseProgress;

public sealed class GetReleaseProgressHandler(
    IDashboardRepository dashboardRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<GetReleaseProgressQuery, Result<ReleaseProgressDto>>
{
    public async Task<Result<ReleaseProgressDto>> Handle(GetReleaseProgressQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Project_View, ct))
            return Result.Failure<ReleaseProgressDto>("Access denied");

        var data = await dashboardRepository.GetReleaseProgressAsync(request.ReleaseId, ct);
        return Result.Success(data);
    }
}
