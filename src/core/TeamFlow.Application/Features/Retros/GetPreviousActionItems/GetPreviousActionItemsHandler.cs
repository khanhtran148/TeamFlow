using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Retros.GetPreviousActionItems;

public sealed class GetPreviousActionItemsHandler(
    IRetroSessionRepository retroRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<GetPreviousActionItemsQuery, Result<IReadOnlyList<RetroActionItemDto>>>
{
    public async Task<Result<IReadOnlyList<RetroActionItemDto>>> Handle(
        GetPreviousActionItemsQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Retro_View, ct))
            return DomainError.Forbidden<IReadOnlyList<RetroActionItemDto>>();

        var lastClosed = await retroRepo.GetLastClosedByProjectAsync(request.ProjectId, ct);
        if (lastClosed is null)
            return Result.Success<IReadOnlyList<RetroActionItemDto>>([]);

        var actionItems = lastClosed.ActionItems
            .Select(RetroMapper.ToActionItemDto)
            .ToList();

        return Result.Success<IReadOnlyList<RetroActionItemDto>>(actionItems);
    }
}
