using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Projects.CreateProject;

public sealed class CreateProjectHandler(
    IProjectRepository projectRepository,
    IProjectMembershipRepository membershipRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
{
    public async Task<Result<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken ct)
    {
        // Permission check against OrgId — no ProjectId exists yet
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.OrgId, Permission.Org_Admin, ct))
            return Result.Failure<ProjectDto>("Access denied");

        var project = new Project
        {
            OrgId = request.OrgId,
            Name = request.Name,
            Description = request.Description,
            Status = "Active"
        };

        await projectRepository.AddAsync(project, ct);

        // Auto-assign creator as OrgAdmin on the new project
        await membershipRepository.AddAsync(new ProjectMembership
        {
            ProjectId = project.Id,
            MemberId = currentUser.Id,
            MemberType = "User",
            Role = Domain.Enums.ProjectRole.OrgAdmin
        }, ct);

        return Result.Success(MapToDto(project, 0, 0));
    }

    private static ProjectDto MapToDto(Project project, int epicCount, int openItemCount) =>
        new(
            project.Id,
            project.OrgId,
            project.Name,
            project.Description,
            project.Status,
            epicCount,
            openItemCount,
            project.CreatedAt,
            project.UpdatedAt
        );
}
