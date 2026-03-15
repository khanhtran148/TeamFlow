using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Sprints.DeleteSprint;

public sealed record DeleteSprintCommand(Guid SprintId) : IRequest<Result>;
