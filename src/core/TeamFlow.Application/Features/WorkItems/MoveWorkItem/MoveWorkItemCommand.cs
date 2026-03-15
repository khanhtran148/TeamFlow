using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.WorkItems.MoveWorkItem;

public sealed record MoveWorkItemCommand(
    Guid WorkItemId,
    Guid? NewParentId
) : IRequest<Result>;
