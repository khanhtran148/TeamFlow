using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Sprints.ListSprints;

public sealed class ListSprintsHandler(
    ISprintRepository sprintRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<ListSprintsQuery, Result<ListSprintsResult>>
{
    public async Task<Result<ListSprintsResult>> Handle(ListSprintsQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Project_View, ct))
            return Result.Failure<ListSprintsResult>("Access denied");

        var (items, totalCount) = await sprintRepository.ListByProjectPagedAsync(
            request.ProjectId, request.Page, request.PageSize, ct);

        var dtos = items.Select(SprintMapper.ToDto).ToList();

        return Result.Success(new ListSprintsResult(dtos, totalCount, request.Page, request.PageSize));
    }
}
