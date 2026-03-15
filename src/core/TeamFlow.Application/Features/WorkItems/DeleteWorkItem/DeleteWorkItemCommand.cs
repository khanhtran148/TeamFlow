using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.WorkItems.DeleteWorkItem;

public sealed record DeleteWorkItemCommand(Guid WorkItemId) : IRequest<Result>;
