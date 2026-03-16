using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Features.Reports.ListSprintReports;

public sealed record ListSprintReportsQuery(
    Guid ProjectId,
    int Page = 1,
    int PageSize = 10
) : IRequest<Result<PagedResult<SprintReportDto>>>;
