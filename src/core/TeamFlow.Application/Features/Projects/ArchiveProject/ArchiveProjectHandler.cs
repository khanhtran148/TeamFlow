using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Projects.ArchiveProject;

public sealed class ArchiveProjectHandler(
    IProjectRepository projectRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<ArchiveProjectCommand, Result>
{
    public async Task<Result> Handle(ArchiveProjectCommand request, CancellationToken ct)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, ct);
        if (project is null)
            return Result.Failure("Project not found");

        project.Status = "Archived";
        await projectRepository.UpdateAsync(project, ct);

        return Result.Success();
    }
}
