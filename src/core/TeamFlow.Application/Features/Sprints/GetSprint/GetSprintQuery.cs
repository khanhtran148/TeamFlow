using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Sprints.GetSprint;

public sealed record GetSprintQuery(Guid SprintId) : IRequest<Result<SprintDetailDto>>;
