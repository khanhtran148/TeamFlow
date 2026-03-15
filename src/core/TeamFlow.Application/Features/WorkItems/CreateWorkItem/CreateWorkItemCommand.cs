using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.WorkItems;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.WorkItems.CreateWorkItem;

public sealed record CreateWorkItemCommand(
    Guid ProjectId,
    Guid? ParentId,
    WorkItemType Type,
    string Title,
    string? Description,
    Priority? Priority,
    string? AcceptanceCriteria
) : IRequest<Result<WorkItemDto>>;
