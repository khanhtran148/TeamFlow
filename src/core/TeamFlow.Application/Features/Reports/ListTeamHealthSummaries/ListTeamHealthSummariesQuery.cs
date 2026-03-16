using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Features.Reports.ListTeamHealthSummaries;

public sealed record ListTeamHealthSummariesQuery(
    Guid ProjectId,
    int Page = 1,
    int PageSize = 10
) : IRequest<Result<PagedResult<TeamHealthSummaryDto>>>;
