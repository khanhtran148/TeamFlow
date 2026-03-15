using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Projects;

namespace TeamFlow.Application.Features.Projects.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid ProjectId,
    string Name,
    string? Description
) : IRequest<Result<ProjectDto>>;
