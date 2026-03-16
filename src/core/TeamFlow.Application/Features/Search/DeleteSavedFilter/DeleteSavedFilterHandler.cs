using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Search.DeleteSavedFilter;

public sealed class DeleteSavedFilterHandler(
    ISavedFilterRepository savedFilterRepository,
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser)
    : IRequestHandler<DeleteSavedFilterCommand, Result>
{
    public async Task<Result> Handle(DeleteSavedFilterCommand request, CancellationToken ct)
    {
        if (!await permissionChecker.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.WorkItem_View, ct))
            return Result.Failure("Access denied");

        var filter = await savedFilterRepository.GetByIdAsync(request.FilterId, ct);
        if (filter is null)
            return Result.Failure("Saved filter not found");

        if (filter.UserId != currentUser.Id)
            return Result.Failure("Access denied");

        await savedFilterRepository.DeleteAsync(request.FilterId, ct);
        return Result.Success();
    }
}
