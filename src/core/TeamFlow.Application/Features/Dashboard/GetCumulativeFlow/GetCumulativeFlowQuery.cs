using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Dashboard.Dtos;

namespace TeamFlow.Application.Features.Dashboard.GetCumulativeFlow;

public sealed record GetCumulativeFlowQuery(
    Guid ProjectId,
    DateOnly FromDate,
    DateOnly ToDate
) : IRequest<Result<CumulativeFlowDto>>;
