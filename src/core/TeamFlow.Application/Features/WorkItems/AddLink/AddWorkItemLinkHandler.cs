using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.WorkItems.AddLink;

public sealed class AddWorkItemLinkHandler(
    IWorkItemRepository workItemRepository,
    IWorkItemLinkRepository linkRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<AddWorkItemLinkCommand, Result>
{
    // Bidirectional reverse mapping
    private static readonly Dictionary<LinkType, LinkType> ReverseMap = new()
    {
        [LinkType.Blocks]    = LinkType.Blocks,    // Stored as reverse Blocks with swapped src/tgt
        [LinkType.RelatesTo] = LinkType.RelatesTo,  // Symmetric
        [LinkType.Duplicates] = LinkType.Duplicates,
        [LinkType.DependsOn]  = LinkType.DependsOn,
        [LinkType.Causes]     = LinkType.Causes,
        [LinkType.Clones]     = LinkType.Clones
    };

    private static readonly HashSet<LinkType> CircularCheckTypes = [LinkType.Blocks, LinkType.DependsOn];

    public async Task<Result> Handle(AddWorkItemLinkCommand request, CancellationToken ct)
    {
        var source = await workItemRepository.GetByIdAsync(request.SourceId, ct);
        if (source is null)
            return Result.Failure("Source work item not found");

        var target = await workItemRepository.GetByIdAsync(request.TargetId, ct);
        if (target is null)
            return Result.Failure("Target work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, source.ProjectId, Permission.WorkItem_ManageLinks, ct))
            return Result.Failure("Access denied");

        // Circular detection for Blocks and DependsOn (must run before duplicate check
        // because the reverse link stored from a prior A→B add will look like a duplicate for B→A)
        if (CircularCheckTypes.Contains(request.LinkType))
        {
            var reachable = await linkRepository.GetReachableTargetsAsync(request.TargetId, request.LinkType, ct);
            if (reachable.Contains(request.SourceId))
                return Result.Failure($"Circular {request.LinkType} detected — adding this link would create a cycle");
        }

        // Check for duplicate (forward direction)
        if (await linkRepository.ExistsAsync(request.SourceId, request.TargetId, request.LinkType, ct))
            return Result.Failure("Link already exists between these items");

        // Determine scope
        var scope = source.ProjectId == target.ProjectId ? LinkScope.SameProject : LinkScope.CrossProject;

        // Create forward link
        var forwardLink = new WorkItemLink
        {
            SourceId = request.SourceId,
            TargetId = request.TargetId,
            LinkType = request.LinkType,
            Scope = scope,
            CreatedById = currentUser.Id
        };

        // Create reverse link (swap source/target)
        var reverseLink = new WorkItemLink
        {
            SourceId = request.TargetId,
            TargetId = request.SourceId,
            LinkType = ReverseMap[request.LinkType],
            Scope = scope,
            CreatedById = currentUser.Id
        };

        await linkRepository.AddRangeAsync([forwardLink, reverseLink], ct);

        // Record history on both items
        await historyService.RecordAsync(new WorkItemHistoryEntry(
            request.SourceId, currentUser.Id, "LinkAdded", "Link",
            null, $"{request.LinkType}:{request.TargetId}"), ct);

        await historyService.RecordAsync(new WorkItemHistoryEntry(
            request.TargetId, currentUser.Id, "LinkAdded", "Link",
            null, $"{ReverseMap[request.LinkType]}:{request.SourceId}"), ct);

        await publisher.Publish(new WorkItemLinkAddedDomainEvent(
            request.SourceId, request.TargetId, request.LinkType, currentUser.Id), ct);

        return Result.Success();
    }
}
