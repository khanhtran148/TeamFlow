using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Admin.ListUsers;

public sealed class AdminListUsersHandler(
    IUserRepository userRepository,
    ICurrentUser currentUser)
    : IRequestHandler<AdminListUsersQuery, Result<PagedResult<AdminUserDto>>>
{
    public async Task<Result<PagedResult<AdminUserDto>>> Handle(
        AdminListUsersQuery request, CancellationToken ct)
    {
        if (currentUser.SystemRole != SystemRole.SystemAdmin)
            return DomainError.Forbidden<PagedResult<AdminUserDto>>("Access forbidden.");

        var (users, totalCount) = await userRepository.ListPagedAsync(
            request.Search, request.Page, request.PageSize, ct);

        var dtos = users.Select(u => new AdminUserDto(
            u.Id,
            u.Email,
            u.Name,
            u.SystemRole,
            u.CreatedAt,
            u.IsActive,
            u.MustChangePassword));

        return Result.Success(new PagedResult<AdminUserDto>(dtos, totalCount, request.Page, request.PageSize));
    }
}
