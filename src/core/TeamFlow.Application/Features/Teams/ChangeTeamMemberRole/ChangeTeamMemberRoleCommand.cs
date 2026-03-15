using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Teams;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Teams.ChangeTeamMemberRole;

public sealed record ChangeTeamMemberRoleCommand(
    Guid TeamId,
    Guid UserId,
    ProjectRole NewRole
) : IRequest<Result<TeamMemberDto>>;
