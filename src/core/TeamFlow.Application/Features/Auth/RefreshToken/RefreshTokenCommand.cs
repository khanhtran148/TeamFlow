using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Auth.RefreshToken;

public sealed record RefreshTokenCommand(
    string Token
) : IRequest<Result<AuthResponse>>;
