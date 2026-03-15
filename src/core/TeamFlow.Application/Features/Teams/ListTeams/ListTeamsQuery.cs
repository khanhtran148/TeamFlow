using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Teams;

namespace TeamFlow.Application.Features.Teams.ListTeams;

public sealed record ListTeamsQuery(
    Guid OrgId,
    int Page,
    int PageSize
) : IRequest<Result<PagedResult<TeamDto>>>;
