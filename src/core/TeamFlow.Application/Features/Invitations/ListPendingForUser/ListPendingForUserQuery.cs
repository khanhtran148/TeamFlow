using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Invitations.ListPendingForUser;

public sealed record ListPendingForUserQuery : IRequest<Result<IEnumerable<InvitationDto>>>;
