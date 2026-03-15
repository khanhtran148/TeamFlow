using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Auth.RefreshToken;

public sealed class RefreshTokenHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IAuthService authService)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private const string InvalidToken = "Invalid or expired refresh token";

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var tokenHash = authService.HashToken(request.Token);
        var existing = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);

        if (existing is null || !existing.IsActive)
            return Result.Failure<AuthResponse>(InvalidToken);

        var user = await userRepository.GetByIdAsync(existing.UserId, ct);
        if (user is null)
            return Result.Failure<AuthResponse>(InvalidToken);

        // Rotate: revoke old, create new
        var newRawToken = authService.GenerateRefreshToken();
        var newTokenHash = authService.HashToken(newRawToken);

        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByTokenHash = newTokenHash;
        await refreshTokenRepository.UpdateAsync(existing, ct);

        var expiresAt = DateTime.UtcNow.AddDays(7);
        var newRefreshToken = new Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAt = expiresAt
        };

        await refreshTokenRepository.AddAsync(newRefreshToken, ct);

        var accessToken = authService.GenerateJwt(user);

        return Result.Success(new AuthResponse(accessToken, newRawToken, expiresAt));
    }
}
