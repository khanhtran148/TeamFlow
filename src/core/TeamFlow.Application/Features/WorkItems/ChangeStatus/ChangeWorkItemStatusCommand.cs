using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.WorkItems;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.WorkItems.ChangeStatus;

public sealed record ChangeWorkItemStatusCommand(
    Guid WorkItemId,
    WorkItemStatus NewStatus
) : IRequest<Result<WorkItemDto>>;
