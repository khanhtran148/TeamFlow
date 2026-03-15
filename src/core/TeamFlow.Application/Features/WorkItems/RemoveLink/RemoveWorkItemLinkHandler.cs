using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.WorkItems.RemoveLink;

public sealed class RemoveWorkItemLinkHandler(
    IWorkItemRepository workItemRepository,
    IWorkItemLinkRepository linkRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<RemoveWorkItemLinkCommand, Result>
{
    public async Task<Result> Handle(RemoveWorkItemLinkCommand request, CancellationToken ct)
    {
        var link = await linkRepository.GetByIdAsync(request.LinkId, ct);
        if (link is null)
            return Result.Failure("Link not found");

        var sourceItem = await workItemRepository.GetByIdAsync(link.SourceId, ct);
        if (!await permissions.HasPermissionAsync(
            currentUser.Id,
            sourceItem?.ProjectId ?? Guid.Empty,
            Permission.WorkItem_ManageLinks, ct))
            return Result.Failure("Access denied");

        // Remove both directions
        await linkRepository.DeletePairAsync(link.SourceId, link.TargetId, ct);

        // Record history on both items
        await historyService.RecordAsync(new WorkItemHistoryEntry(
            link.SourceId, currentUser.Id, "LinkRemoved", "Link",
            $"{link.LinkType}:{link.TargetId}", null), ct);

        await historyService.RecordAsync(new WorkItemHistoryEntry(
            link.TargetId, currentUser.Id, "LinkRemoved", "Link",
            $"{link.LinkType}:{link.SourceId}", null), ct);

        await publisher.Publish(new WorkItemLinkRemovedDomainEvent(
            link.SourceId, link.TargetId, link.LinkType, currentUser.Id), ct);

        return Result.Success();
    }
}
