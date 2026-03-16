using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Admin.ResetUserPassword;

public sealed class AdminResetUserPasswordHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthService authService,
    ICurrentUser currentUser)
    : IRequestHandler<AdminResetUserPasswordCommand, Result>
{
    public async Task<Result> Handle(AdminResetUserPasswordCommand request, CancellationToken ct)
    {
        if (currentUser.SystemRole != SystemRole.SystemAdmin)
            return DomainError.Forbidden("Access forbidden.");

        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return DomainError.NotFound("User not found.");

        user.PasswordHash = authService.HashPassword(request.NewPassword);
        user.MustChangePassword = true;

        await userRepository.UpdateAsync(user, ct);
        await refreshTokenRepository.RevokeAllForUserAsync(user.Id, ct);

        return Result.Success();
    }
}
