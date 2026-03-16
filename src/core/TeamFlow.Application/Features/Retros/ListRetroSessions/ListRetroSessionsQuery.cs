using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Retros.ListRetroSessions;

public sealed record ListRetroSessionsQuery(
    Guid ProjectId,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<ListRetroSessionsResponse>>;

public sealed record ListRetroSessionsResponse(
    IReadOnlyList<RetroSessionSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
