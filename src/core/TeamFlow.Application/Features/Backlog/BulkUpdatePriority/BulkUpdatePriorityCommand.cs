using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Backlog.BulkUpdatePriority;

public sealed record BulkUpdatePriorityCommand(
    IReadOnlyList<PriorityUpdate> Items
) : IRequest<Result>;

public sealed record PriorityUpdate(Guid WorkItemId, Priority Priority);
