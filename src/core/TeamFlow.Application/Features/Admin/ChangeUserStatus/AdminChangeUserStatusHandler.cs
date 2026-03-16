using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Admin.ChangeUserStatus;

public sealed class AdminChangeUserStatusHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ICurrentUser currentUser)
    : IRequestHandler<AdminChangeUserStatusCommand, Result>
{
    public async Task<Result> Handle(AdminChangeUserStatusCommand request, CancellationToken ct)
    {
        if (currentUser.SystemRole != SystemRole.SystemAdmin)
            return DomainError.Forbidden("Access forbidden.");

        if (request.UserId == currentUser.Id && !request.IsActive)
            return DomainError.Forbidden("Cannot deactivate your own account.");

        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return DomainError.NotFound("User not found.");

        // Prevent deactivating the last active SystemAdmin
        if (!request.IsActive && user.SystemRole == SystemRole.SystemAdmin)
        {
            var allUsers = await userRepository.ListAllAsync(ct);
            var activeAdminCount = allUsers.Count(u =>
                u.SystemRole == SystemRole.SystemAdmin &&
                u.IsActive &&
                u.Id != request.UserId);

            if (activeAdminCount == 0)
                return DomainError.Forbidden("Cannot deactivate the last system administrator.");
        }

        user.IsActive = request.IsActive;
        await userRepository.UpdateAsync(user, ct);

        if (!request.IsActive)
            await refreshTokenRepository.RevokeAllForUserAsync(user.Id, ct);

        return Result.Success();
    }
}
