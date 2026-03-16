using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Dashboard.Dtos;

namespace TeamFlow.Application.Features.Dashboard.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery(
    Guid ProjectId
) : IRequest<Result<DashboardSummaryDto>>;
