using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Users.GetProfile;

public sealed class GetProfileHandler(
    IUserRepository userRepository,
    IOrganizationMemberRepository orgMemberRepository,
    ITeamMemberRepository teamMemberRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetProfileQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(GetProfileQuery request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(currentUser.Id, ct);
        if (user is null)
            return Result.Failure<UserProfileDto>("User not found");

        var orgMemberships = await orgMemberRepository.ListOrganizationsForUserAsync(currentUser.Id, ct);
        var teamMemberships = await teamMemberRepository.ListTeamsForUserAsync(currentUser.Id, ct);

        var orgDtos = orgMemberships
            .Select(m => new ProfileOrganizationDto(
                m.Org.Id,
                m.Org.Name,
                m.Org.Slug,
                m.Role.ToString(),
                m.JoinedAt))
            .ToList()
            .AsReadOnly();

        var teamDtos = teamMemberships
            .Select(m => new ProfileTeamDto(
                m.Team.Id,
                m.Team.Name,
                m.Team.OrgId,
                m.Team.Organization?.Name ?? string.Empty,
                m.Role.ToString(),
                m.JoinedAt))
            .ToList()
            .AsReadOnly();

        return Result.Success(new UserProfileDto(
            user.Id,
            user.Email,
            user.Name,
            user.AvatarUrl,
            user.SystemRole.ToString(),
            user.CreatedAt,
            orgDtos,
            teamDtos));
    }
}
