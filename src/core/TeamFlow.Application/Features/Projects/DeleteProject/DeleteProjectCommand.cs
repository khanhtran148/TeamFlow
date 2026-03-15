using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Projects.DeleteProject;

public sealed record DeleteProjectCommand(Guid ProjectId) : IRequest<Result>;
