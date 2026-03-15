using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.WorkItems;

namespace TeamFlow.Application.Features.WorkItems.GetWorkItem;

public sealed record GetWorkItemQuery(Guid WorkItemId) : IRequest<Result<WorkItemDto>>;
