using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Invitations.Accept;

public sealed record AcceptInvitationCommand(string Token) : IRequest<Result<AcceptInvitationResponse>>;
