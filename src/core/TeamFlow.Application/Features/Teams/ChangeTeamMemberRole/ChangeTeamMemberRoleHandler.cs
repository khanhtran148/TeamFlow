using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Teams.ChangeTeamMemberRole;

public sealed class ChangeTeamMemberRoleHandler(
    ITeamRepository teamRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<ChangeTeamMemberRoleCommand, Result<TeamMemberDto>>
{
    public async Task<Result<TeamMemberDto>> Handle(ChangeTeamMemberRoleCommand request, CancellationToken ct)
    {
        var team = await teamRepository.GetByIdWithMembersAsync(request.TeamId, ct);
        if (team is null)
            return Result.Failure<TeamMemberDto>("Team not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, team.OrgId, Permission.Team_Manage, ct))
            return Result.Failure<TeamMemberDto>("Access denied");

        var member = team.Members.FirstOrDefault(m => m.UserId == request.UserId);
        if (member is null)
            return Result.Failure<TeamMemberDto>("Team member not found");

        member.Role = request.NewRole;
        await teamRepository.UpdateAsync(team, ct);

        var user = await userRepository.GetByIdAsync(request.UserId, ct);

        return Result.Success(new TeamMemberDto(
            member.Id,
            member.UserId,
            user?.Name ?? "Unknown",
            user?.Email ?? string.Empty,
            member.Role,
            member.JoinedAt
        ));
    }
}
