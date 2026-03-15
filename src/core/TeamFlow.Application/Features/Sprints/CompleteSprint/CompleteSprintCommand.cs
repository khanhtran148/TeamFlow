using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Sprints.CompleteSprint;

public sealed record CompleteSprintCommand(Guid SprintId) : IRequest<Result<SprintDto>>;
