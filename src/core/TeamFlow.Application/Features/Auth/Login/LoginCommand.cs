using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Auth.Login;

public sealed record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<AuthResponse>>;
