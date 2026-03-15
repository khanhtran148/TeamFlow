using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Sprints.GetBurndown;

public sealed record GetBurndownQuery(Guid SprintId) : IRequest<Result<BurndownDto>>;
