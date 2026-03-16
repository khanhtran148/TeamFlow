using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Auth.ChangePassword;

public sealed class ChangePasswordHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ICurrentUser currentUser,
    IAuthService authService)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(currentUser.Id, ct);
        if (user is null)
            return Result.Failure("User not found");

        if (!authService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return Result.Failure("Current password is incorrect");

        user.PasswordHash = authService.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        await userRepository.UpdateAsync(user, ct);

        // Revoke all existing sessions after password change
        await refreshTokenRepository.RevokeAllForUserAsync(currentUser.Id, ct);

        return Result.Success();
    }
}
