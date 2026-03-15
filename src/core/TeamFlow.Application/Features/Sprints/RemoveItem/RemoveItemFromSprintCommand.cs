using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Sprints.RemoveItem;

public sealed record RemoveItemFromSprintCommand(
    Guid SprintId,
    Guid WorkItemId
) : IRequest<Result>;
