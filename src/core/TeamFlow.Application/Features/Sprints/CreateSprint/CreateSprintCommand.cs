using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Sprints.CreateSprint;

public sealed record CreateSprintCommand(
    Guid ProjectId,
    string Name,
    string? Goal,
    DateOnly? StartDate,
    DateOnly? EndDate
) : IRequest<Result<SprintDto>>;
