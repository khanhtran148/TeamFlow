using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Search.UpdateSavedFilter;

public sealed class UpdateSavedFilterHandler(
    ISavedFilterRepository savedFilterRepository,
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateSavedFilterCommand, Result<SavedFilterDto>>
{
    public async Task<Result<SavedFilterDto>> Handle(UpdateSavedFilterCommand request, CancellationToken ct)
    {
        if (!await permissionChecker.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.WorkItem_View, ct))
            return Result.Failure<SavedFilterDto>("Access denied");

        var filter = await savedFilterRepository.GetByIdAsync(request.FilterId, ct);
        if (filter is null)
            return Result.Failure<SavedFilterDto>("Saved filter not found");

        if (filter.UserId != currentUser.Id)
            return Result.Failure<SavedFilterDto>("Access denied");

        if (request.Name is not null && request.Name != filter.Name)
        {
            if (await savedFilterRepository.ExistsByNameAsync(currentUser.Id, request.ProjectId, request.Name, ct))
                return Result.Failure<SavedFilterDto>("A saved filter with this name already exists");

            filter.Name = request.Name;
        }

        if (request.FilterJson is not null)
            filter.FilterJson = request.FilterJson;

        if (request.IsDefault.HasValue)
            filter.IsDefault = request.IsDefault.Value;

        await savedFilterRepository.UpdateAsync(filter, ct);

        return Result.Success(new SavedFilterDto(
            filter.Id, filter.Name, filter.FilterJson, filter.IsDefault, filter.CreatedAt));
    }
}
