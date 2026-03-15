using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Sprints.UpdateSprint;

public sealed record UpdateSprintCommand(
    Guid SprintId,
    string Name,
    string? Goal,
    DateOnly? StartDate,
    DateOnly? EndDate
) : IRequest<Result<SprintDto>>;
