using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Admin.ListUsers;

public sealed record AdminListUsersQuery
    : IRequest<Result<IEnumerable<AdminUserDto>>>;
