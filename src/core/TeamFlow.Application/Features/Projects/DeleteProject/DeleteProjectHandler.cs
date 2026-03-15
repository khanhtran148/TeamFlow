using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Projects.DeleteProject;

public sealed class DeleteProjectHandler(
    IProjectRepository projectRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<DeleteProjectCommand, Result>
{
    public async Task<Result> Handle(DeleteProjectCommand request, CancellationToken ct)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, ct);
        if (project is null)
            return Result.Failure("Project not found");

        project.Status = "Deleted";
        await projectRepository.UpdateAsync(project, ct);

        return Result.Success();
    }
}
