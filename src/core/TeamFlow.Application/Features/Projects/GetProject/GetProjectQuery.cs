using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Projects;

namespace TeamFlow.Application.Features.Projects.GetProject;

public sealed record GetProjectQuery(Guid ProjectId) : IRequest<Result<ProjectDto>>;
