using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.ProjectMemberships.RemoveProjectMember;

public sealed class RemoveProjectMemberHandler(
    IProjectMembershipRepository membershipRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<RemoveProjectMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveProjectMemberCommand request, CancellationToken ct)
    {
        var membership = await membershipRepository.GetByIdAsync(request.MembershipId, ct);
        if (membership is null)
            return Result.Failure("Project membership not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, membership.ProjectId, Permission.Project_ManageMembers, ct))
            return Result.Failure("Access denied");

        await membershipRepository.DeleteAsync(membership, ct);

        return Result.Success();
    }
}
