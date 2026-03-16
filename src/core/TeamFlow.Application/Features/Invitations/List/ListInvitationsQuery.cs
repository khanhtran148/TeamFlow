using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Invitations.List;

public sealed record ListInvitationsQuery(Guid OrgId) : IRequest<Result<IEnumerable<InvitationDto>>>;
