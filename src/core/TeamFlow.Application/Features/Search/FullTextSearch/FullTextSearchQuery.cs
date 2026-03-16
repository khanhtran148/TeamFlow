using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.WorkItems;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Search.FullTextSearch;

public sealed record FullTextSearchQuery(
    Guid ProjectId,
    string? Q,
    WorkItemStatus[]? Status,
    Priority[]? Priority,
    WorkItemType[]? Type,
    Guid? AssigneeId,
    Guid? SprintId,
    Guid? ReleaseId,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<WorkItemDto>>>;
