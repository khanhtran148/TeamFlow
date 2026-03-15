using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Auth.Logout;

public sealed class LogoutHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ICurrentUser currentUser)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
    {
        await refreshTokenRepository.RevokeAllForUserAsync(currentUser.Id, ct);
        return Result.Success();
    }
}
