using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Projects.GetProject;

public sealed class GetProjectHandler(IProjectRepository projectRepository)
    : IRequestHandler<GetProjectQuery, Result<ProjectDto>>
{
    public async Task<Result<ProjectDto>> Handle(GetProjectQuery request, CancellationToken ct)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, ct);
        if (project is null)
            return Result.Failure<ProjectDto>("Project not found");

        var epicCount = await projectRepository.CountEpicsAsync(project.Id, ct);
        var openItemCount = await projectRepository.CountOpenWorkItemsAsync(project.Id, ct);

        return Result.Success(new ProjectDto(
            project.Id,
            project.OrgId,
            project.Name,
            project.Description,
            project.Status,
            epicCount,
            openItemCount,
            project.CreatedAt,
            project.UpdatedAt
        ));
    }
}
