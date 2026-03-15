using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.WorkItems.GetLinks;

public sealed class GetWorkItemLinksHandler(
    IWorkItemRepository workItemRepository,
    IWorkItemLinkRepository linkRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<GetWorkItemLinksQuery, Result<WorkItemLinksDto>>
{
    public async Task<Result<WorkItemLinksDto>> Handle(GetWorkItemLinksQuery request, CancellationToken ct)
    {
        var item = await workItemRepository.GetByIdAsync(request.WorkItemId, ct);
        if (item is null)
            return Result.Failure<WorkItemLinksDto>("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, item.ProjectId, Permission.WorkItem_View, ct))
            return Result.Failure<WorkItemLinksDto>("Access denied");

        var links = await linkRepository.GetLinksForItemAsync(request.WorkItemId, ct);

        var groups = links
            .GroupBy(l => l.LinkType)
            .Select(g => new LinkGroupDto(
                g.Key,
                g.Select(l =>
                {
                    // Determine the "other" item
                    var otherItem = l.SourceId == request.WorkItemId ? l.Target : l.Source;
                    return otherItem is null
                        ? null
                        : new LinkedItemDto(otherItem.Id, otherItem.Title, otherItem.Type, otherItem.Status, l.Scope);
                })
                .Where(x => x is not null)
                .Select(x => x!)
            ))
            .ToList();

        return Result.Success(new WorkItemLinksDto(request.WorkItemId, groups));
    }
}
