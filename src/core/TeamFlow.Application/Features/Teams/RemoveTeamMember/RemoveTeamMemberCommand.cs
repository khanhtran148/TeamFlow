using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Teams.RemoveTeamMember;

public sealed record RemoveTeamMemberCommand(Guid TeamId, Guid UserId) : IRequest<Result>;
