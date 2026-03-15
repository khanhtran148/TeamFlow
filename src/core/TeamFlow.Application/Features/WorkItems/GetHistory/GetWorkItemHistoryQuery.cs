using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Features.WorkItems.GetHistory;

public sealed record GetWorkItemHistoryQuery(
    Guid WorkItemId,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<WorkItemHistoryDto>>>;

public sealed record WorkItemHistoryDto(
    Guid Id,
    Guid? ActorId,
    string? ActorName,
    string ActorType,
    string ActionType,
    string? FieldName,
    string? OldValue,
    string? NewValue,
    DateTime CreatedAt);
