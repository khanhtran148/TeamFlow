using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Invitations.ListPendingForUser;

public sealed class ListPendingForUserHandler(
    IInvitationRepository invitationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ListPendingForUserQuery, Result<IEnumerable<InvitationDto>>>
{
    public async Task<Result<IEnumerable<InvitationDto>>> Handle(
        ListPendingForUserQuery request, CancellationToken ct)
    {
        var invitations = await invitationRepository.ListPendingByEmailAsync(currentUser.Email, ct);

        var now = DateTime.UtcNow;
        var dtos = invitations
            .Where(i => i.Status == InviteStatus.Pending && i.ExpiresAt > now)
            .Select(i => new InvitationDto(
                i.Id,
                i.OrganizationId,
                i.Email,
                i.Role,
                i.Status,
                i.ExpiresAt,
                i.CreatedAt,
                i.AcceptedBy?.Name))
            .ToList();

        return Result.Success<IEnumerable<InvitationDto>>(dtos);
    }
}
