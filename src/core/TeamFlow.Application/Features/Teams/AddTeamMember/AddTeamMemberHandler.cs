using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Teams.AddTeamMember;

public sealed class AddTeamMemberHandler(
    ITeamRepository teamRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<AddTeamMemberCommand, Result>
{
    public async Task<Result> Handle(AddTeamMemberCommand request, CancellationToken ct)
    {
        var team = await teamRepository.GetByIdWithMembersAsync(request.TeamId, ct);
        if (team is null)
            return Result.Failure("Team not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, team.OrgId, Permission.Team_Manage, ct))
            return Result.Failure("Access denied");

        if (team.Members.Any(m => m.UserId == request.UserId))
            return Result.Failure("User is already a member of this team");

        team.Members.Add(new TeamMember
        {
            TeamId = team.Id,
            UserId = request.UserId,
            Role = request.Role
        });

        await teamRepository.UpdateAsync(team, ct);

        return Result.Success();
    }
}
