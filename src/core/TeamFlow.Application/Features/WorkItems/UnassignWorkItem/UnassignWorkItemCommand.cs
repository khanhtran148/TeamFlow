using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.WorkItems.UnassignWorkItem;

public sealed record UnassignWorkItemCommand(Guid WorkItemId) : IRequest<Result>;
