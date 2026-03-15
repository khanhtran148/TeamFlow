using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Teams.AddTeamMember;

public sealed record AddTeamMemberCommand(
    Guid TeamId,
    Guid UserId,
    ProjectRole Role
) : IRequest<Result>;
