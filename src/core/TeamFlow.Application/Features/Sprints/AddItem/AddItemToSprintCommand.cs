using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Sprints.AddItem;

public sealed record AddItemToSprintCommand(
    Guid SprintId,
    Guid WorkItemId
) : IRequest<Result>;
