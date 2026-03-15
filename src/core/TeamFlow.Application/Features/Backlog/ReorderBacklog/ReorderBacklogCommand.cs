using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Backlog.ReorderBacklog;

public sealed record ReorderBacklogCommand(
    Guid ProjectId,
    IEnumerable<WorkItemSortOrder> Items
) : IRequest<Result>;

public sealed record WorkItemSortOrder(Guid WorkItemId, int SortOrder);
