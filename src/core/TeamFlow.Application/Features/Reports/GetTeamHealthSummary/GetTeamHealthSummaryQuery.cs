using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Reports.GetTeamHealthSummary;

public sealed record GetTeamHealthSummaryQuery(
    Guid ProjectId
) : IRequest<Result<TeamHealthSummaryDto>>;
