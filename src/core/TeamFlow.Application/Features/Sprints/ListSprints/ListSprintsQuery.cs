using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Sprints.ListSprints;

public sealed record ListSprintsQuery(
    Guid ProjectId,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<ListSprintsResult>>;

public sealed record ListSprintsResult(
    IReadOnlyList<SprintDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
