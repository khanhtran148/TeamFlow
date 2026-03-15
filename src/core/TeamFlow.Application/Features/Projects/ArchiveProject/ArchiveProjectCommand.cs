using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Projects.ArchiveProject;

public sealed record ArchiveProjectCommand(Guid ProjectId) : IRequest<Result>;
