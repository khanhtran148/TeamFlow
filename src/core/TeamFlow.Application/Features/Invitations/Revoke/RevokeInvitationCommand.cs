using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Invitations.Revoke;

public sealed record RevokeInvitationCommand(Guid InvitationId) : IRequest<Result>;
