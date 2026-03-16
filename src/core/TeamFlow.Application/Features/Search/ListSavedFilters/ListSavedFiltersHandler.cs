using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Search.ListSavedFilters;

public sealed class ListSavedFiltersHandler(
    ISavedFilterRepository savedFilterRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<ListSavedFiltersQuery, Result<IReadOnlyList<SavedFilterDto>>>
{
    public async Task<Result<IReadOnlyList<SavedFilterDto>>> Handle(
        ListSavedFiltersQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.WorkItem_View, ct))
            return Result.Failure<IReadOnlyList<SavedFilterDto>>("Access denied");

        var filters = await savedFilterRepository.ListByUserAndProjectAsync(currentUser.Id, request.ProjectId, ct);

        var dtos = filters.Select(f => new SavedFilterDto(
            f.Id, f.Name, f.FilterJson, f.IsDefault, f.CreatedAt
        )).ToList();

        return Result.Success<IReadOnlyList<SavedFilterDto>>(dtos);
    }
}
