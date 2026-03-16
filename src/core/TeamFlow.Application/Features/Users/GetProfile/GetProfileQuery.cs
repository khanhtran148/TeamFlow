using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Users.GetProfile;

public sealed record GetProfileQuery : IRequest<Result<UserProfileDto>>;
