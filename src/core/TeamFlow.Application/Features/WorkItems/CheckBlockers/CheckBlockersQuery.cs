using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.WorkItems.CheckBlockers;

public sealed record CheckBlockersQuery(Guid WorkItemId) : IRequest<Result<BlockersDto>>;

public sealed record BlockersDto(
    Guid WorkItemId,
    bool HasUnresolvedBlockers,
    IEnumerable<BlockerItemDto> Blockers
);

public sealed record BlockerItemDto(
    Guid BlockerId,
    string Title,
    WorkItemStatus Status
);
