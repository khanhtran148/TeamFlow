using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
namespace TeamFlow.Application.Features.Auth.Login;

public sealed class LoginHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthService authService)
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private const string InvalidCredentials = "Invalid email or password";

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), ct);
        if (user is null)
            return Result.Failure<AuthResponse>(InvalidCredentials);

        if (!authService.VerifyPassword(request.Password, user.PasswordHash))
            return Result.Failure<AuthResponse>(InvalidCredentials);

        var accessToken = authService.GenerateJwt(user);
        var rawRefreshToken = authService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var refreshToken = new Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            TokenHash = authService.HashToken(rawRefreshToken),
            ExpiresAt = expiresAt
        };

        await refreshTokenRepository.AddAsync(refreshToken, ct);

        return Result.Success(new AuthResponse(accessToken, rawRefreshToken, expiresAt));
    }
}
