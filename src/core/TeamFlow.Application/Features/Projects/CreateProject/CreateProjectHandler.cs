using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Projects.CreateProject;

public sealed class CreateProjectHandler(
    IProjectRepository projectRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
{
    public async Task<Result<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken ct)
    {
        var project = new Project
        {
            OrgId = request.OrgId,
            Name = request.Name,
            Description = request.Description,
            Status = "Active"
        };

        await projectRepository.AddAsync(project, ct);

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
