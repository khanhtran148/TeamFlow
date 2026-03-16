using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Dashboard.Dtos;

namespace TeamFlow.Application.Features.Dashboard.GetVelocityChart;

public sealed record GetVelocityChartQuery(
    Guid ProjectId,
    int SprintCount = 10
) : IRequest<Result<VelocityChartDto>>;
