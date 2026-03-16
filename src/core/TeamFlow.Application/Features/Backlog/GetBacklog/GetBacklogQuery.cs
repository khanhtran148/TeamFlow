using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Models;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Backlog.GetBacklog;

public sealed record GetBacklogQuery(
    Guid ProjectId,
    WorkItemStatus? Status,
    Priority? Priority,
    Guid? AssigneeId,
    WorkItemType? Type,
    Guid? SprintId,
    Guid? ReleaseId,
    bool? Unscheduled,
    string? Search,
    bool? IsReady,
    int Page,
    int PageSize
) : IRequest<Result<PagedResult<BacklogItemDto>>>;

public sealed record BacklogItemDto(
    Guid Id,
    Guid ProjectId,
    Guid? ParentId,
    WorkItemType Type,
    string Title,
    WorkItemStatus Status,
    Priority? Priority,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid? ReleaseId,
    string? ReleaseName,
    bool IsBlocked,
    int SortOrder,
    IEnumerable<BacklogItemDto> Children
);
