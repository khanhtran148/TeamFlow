using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Auth.Logout;

public sealed record LogoutCommand : IRequest<Result>;
