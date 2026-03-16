using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Admin.ListUsers;

public sealed class AdminListUsersHandler(
    IUserRepository userRepository,
    ICurrentUser currentUser)
    : IRequestHandler<AdminListUsersQuery, Result<IEnumerable<AdminUserDto>>>
{
    public async Task<Result<IEnumerable<AdminUserDto>>> Handle(
        AdminListUsersQuery request, CancellationToken ct)
    {
        if (currentUser.SystemRole != SystemRole.SystemAdmin)
            return DomainError.Forbidden<IEnumerable<AdminUserDto>>("Access forbidden.");

        var users = await userRepository.ListAllAsync(ct);

        var dtos = users.Select(u => new AdminUserDto(
            u.Id,
            u.Email,
            u.Name,
            u.SystemRole,
            u.CreatedAt));

        return Result.Success(dtos);
    }
}
