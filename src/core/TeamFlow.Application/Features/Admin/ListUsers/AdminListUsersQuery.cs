using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Features.Admin.ListUsers;

public sealed record AdminListUsersQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<AdminUserDto>>>;
