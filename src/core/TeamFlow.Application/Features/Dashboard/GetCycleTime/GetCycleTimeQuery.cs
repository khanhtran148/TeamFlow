using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Dashboard.Dtos;

namespace TeamFlow.Application.Features.Dashboard.GetCycleTime;

public sealed record GetCycleTimeQuery(
    Guid ProjectId,
    DateOnly? FromDate,
    DateOnly? ToDate
) : IRequest<Result<CycleTimeDto>>;
