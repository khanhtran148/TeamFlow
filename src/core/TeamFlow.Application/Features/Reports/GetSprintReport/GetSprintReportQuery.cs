using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Reports.GetSprintReport;

public sealed record GetSprintReportQuery(
    Guid SprintId,
    Guid ProjectId
) : IRequest<Result<SprintReportDto>>;
