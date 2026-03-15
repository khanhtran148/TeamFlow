using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Sprints.UpdateCapacity;

public sealed record UpdateCapacityCommand(
    Guid SprintId,
    IReadOnlyList<CapacityEntry> Capacity
) : IRequest<Result>;

public sealed record CapacityEntry(Guid MemberId, int Points);
