using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Teams.RemoveTeamMember;

public sealed class RemoveTeamMemberHandler(
    ITeamRepository teamRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<RemoveTeamMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveTeamMemberCommand request, CancellationToken ct)
    {
        var team = await teamRepository.GetByIdWithMembersAsync(request.TeamId, ct);
        if (team is null)
            return Result.Failure("Team not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, team.OrgId, Permission.Team_Manage, ct))
            return Result.Failure("Access denied");

        var member = team.Members.FirstOrDefault(m => m.UserId == request.UserId);
        if (member is null)
            return Result.Failure("Team member not found");

        team.Members.Remove(member);
        await teamRepository.UpdateAsync(team, ct);

        return Result.Success();
    }
}
