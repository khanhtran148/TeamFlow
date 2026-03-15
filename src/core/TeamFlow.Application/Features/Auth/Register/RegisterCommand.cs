using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Auth.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string Name
) : IRequest<Result<AuthResponse>>;
