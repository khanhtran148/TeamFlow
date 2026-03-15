using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
namespace TeamFlow.Application.Features.Auth.Register;

public sealed class RegisterHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthService authService)
    : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken ct)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, ct))
            return Result.Failure<AuthResponse>("A user with this email already exists");

        var user = new Domain.Entities.User
        {
            Email = request.Email.ToLowerInvariant(),
            Name = request.Name,
            PasswordHash = authService.HashPassword(request.Password)
        };

        await userRepository.AddAsync(user, ct);

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
