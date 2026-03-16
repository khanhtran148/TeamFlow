using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Dashboard.Dtos;

namespace TeamFlow.Application.Features.Dashboard.GetWorkloadHeatmap;

public sealed record GetWorkloadHeatmapQuery(
    Guid ProjectId
) : IRequest<Result<WorkloadHeatmapDto>>;
