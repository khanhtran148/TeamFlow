using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Projects.UpdateProject;

public sealed class UpdateProjectHandler(
    IProjectRepository projectRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<UpdateProjectCommand, Result<ProjectDto>>
{
    public async Task<Result<ProjectDto>> Handle(UpdateProjectCommand request, CancellationToken ct)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, ct);
        if (project is null)
            return Result.Failure<ProjectDto>("Project not found");

        project.Name = request.Name;
        project.Description = request.Description;

        await projectRepository.UpdateAsync(project, ct);

        return Result.Success(MapToDto(project));
    }

    private static ProjectDto MapToDto(Project project) =>
        new(
            project.Id,
            project.OrgId,
            project.Name,
            project.Description,
            project.Status,
            0,
            0,
            project.CreatedAt,
            project.UpdatedAt
        );
}
