using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.WorkItems.CheckBlockers;

public sealed class CheckBlockersHandler(
    IWorkItemRepository workItemRepository,
    IWorkItemLinkRepository linkRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<CheckBlockersQuery, Result<BlockersDto>>
{
    public async Task<Result<BlockersDto>> Handle(CheckBlockersQuery request, CancellationToken ct)
    {
        var item = await workItemRepository.GetByIdAsync(request.WorkItemId, ct);
        if (item is null)
            return Result.Failure<BlockersDto>("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, item.ProjectId, Permission.WorkItem_View, ct))
            return Result.Failure<BlockersDto>("Access denied");

        var blockerLinks = await linkRepository.GetBlockersForItemAsync(request.WorkItemId, ct);

        var blockerItems = blockerLinks
            .Where(l => l.Source is not null && l.Source.Status != WorkItemStatus.Done)
            .Select(l => new BlockerItemDto(l.Source!.Id, l.Source.Title, l.Source.Status))
            .ToList();

        return Result.Success(new BlockersDto(
            request.WorkItemId,
            blockerItems.Count > 0,
            blockerItems
        ));
    }
}
