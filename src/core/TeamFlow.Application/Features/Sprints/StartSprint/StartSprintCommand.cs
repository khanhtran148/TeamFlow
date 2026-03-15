using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Sprints.StartSprint;

public sealed record StartSprintCommand(Guid SprintId) : IRequest<Result<SprintDto>>;
