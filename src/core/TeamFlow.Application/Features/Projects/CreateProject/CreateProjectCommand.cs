using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Projects;

namespace TeamFlow.Application.Features.Projects.CreateProject;

public sealed record CreateProjectCommand(
    Guid OrgId,
    string Name,
    string? Description
) : IRequest<Result<ProjectDto>>;
