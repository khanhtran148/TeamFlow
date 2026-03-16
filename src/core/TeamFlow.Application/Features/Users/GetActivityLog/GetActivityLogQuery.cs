using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Features.Users.GetActivityLog;

public sealed record GetActivityLogQuery(
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<ActivityLogItemDto>>>;
