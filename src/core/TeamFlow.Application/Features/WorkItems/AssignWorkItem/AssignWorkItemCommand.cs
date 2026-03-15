using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.WorkItems.AssignWorkItem;

public sealed record AssignWorkItemCommand(
    Guid WorkItemId,
    Guid AssigneeId
) : IRequest<Result>;
