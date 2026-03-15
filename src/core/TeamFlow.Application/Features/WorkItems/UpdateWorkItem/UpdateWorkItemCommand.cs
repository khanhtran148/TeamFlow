using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.WorkItems;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.WorkItems.UpdateWorkItem;

public sealed record UpdateWorkItemCommand(
    Guid WorkItemId,
    string Title,
    string? Description,
    Priority? Priority,
    decimal? EstimationValue,
    string? AcceptanceCriteria
) : IRequest<Result<WorkItemDto>>;
