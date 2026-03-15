using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.ProjectMemberships;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.ProjectMemberships.AddProjectMember;

public sealed class AddProjectMemberHandler(
    IProjectMembershipRepository membershipRepository,
    IProjectRepository projectRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<AddProjectMemberCommand, Result<ProjectMembershipDto>>
{
    public async Task<Result<ProjectMembershipDto>> Handle(AddProjectMemberCommand request, CancellationToken ct)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, ct);
        if (project is null)
            return Result.Failure<ProjectMembershipDto>("Project not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Project_ManageMembers, ct))
            return Result.Failure<ProjectMembershipDto>("Access denied");

        if (await membershipRepository.ExistsAsync(request.ProjectId, request.MemberId, request.MemberType, ct))
            return Result.Failure<ProjectMembershipDto>("Member is already a member of this project");

        var membership = new ProjectMembership
        {
            ProjectId = request.ProjectId,
            MemberId = request.MemberId,
            MemberType = request.MemberType,
            Role = request.Role
        };

        await membershipRepository.AddAsync(membership, ct);

        return Result.Success(new ProjectMembershipDto(
            membership.Id,
            membership.ProjectId,
            membership.MemberId,
            membership.MemberType,
            "Unknown",
            membership.Role,
            membership.CreatedAt
        ));
    }
}
