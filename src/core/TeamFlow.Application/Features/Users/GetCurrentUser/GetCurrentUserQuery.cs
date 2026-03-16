using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Users.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<Result<CurrentUserDto>>;
