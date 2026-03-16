using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Search.SaveFilter;

public sealed class SaveFilterHandler(
    ISavedFilterRepository savedFilterRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<SaveFilterCommand, Result<SavedFilterDto>>
{
    public async Task<Result<SavedFilterDto>> Handle(SaveFilterCommand request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.WorkItem_View, ct))
            return Result.Failure<SavedFilterDto>("Access denied");

        if (await savedFilterRepository.ExistsByNameAsync(currentUser.Id, request.ProjectId, request.Name, ct))
            return Result.Failure<SavedFilterDto>("A saved filter with this name already exists");

        var filter = new SavedFilter
        {
            UserId = currentUser.Id,
            ProjectId = request.ProjectId,
            Name = request.Name,
            FilterJson = request.FilterJson,
            IsDefault = request.IsDefault
        };

        await savedFilterRepository.AddAsync(filter, ct);

        return Result.Success(MapToDto(filter));
    }

    private static SavedFilterDto MapToDto(SavedFilter f) =>
        new(f.Id, f.Name, f.FilterJson, f.IsDefault, f.CreatedAt);
}
