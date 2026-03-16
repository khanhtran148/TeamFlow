using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Users.UpdateProfile;

public sealed record UpdateProfileCommand(
    string Name,
    string? AvatarUrl
) : IRequest<Result<UserProfileDto>>;
