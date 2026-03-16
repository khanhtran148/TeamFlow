using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Invitations.Revoke;

public sealed class RevokeInvitationHandler(
    IInvitationRepository invitationRepository,
    IOrganizationMemberRepository memberRepository,
    ICurrentUser currentUser)
    : IRequestHandler<RevokeInvitationCommand, Result>
{
    public async Task<Result> Handle(RevokeInvitationCommand request, CancellationToken ct)
    {
        // 1. Look up invitation
        var invitation = await invitationRepository.GetByIdAsync(request.InvitationId, ct);
        if (invitation is null)
            return DomainError.NotFound("Invitation not found.");

        // 2. Permission check — Owner or Admin of the org only
        var role = await memberRepository.GetMemberRoleAsync(invitation.OrganizationId, currentUser.Id, ct);
        if (role is null || role == OrgRole.Member)
            return DomainError.Forbidden("Only Org Owner or Admin can revoke invitations.");

        // 3. Cannot revoke already-accepted invitations
        if (invitation.Status == InviteStatus.Accepted)
            return DomainError.Validation("Cannot revoke an invitation that has already been accepted.");

        // 4. Set status to Revoked
        invitation.Status = InviteStatus.Revoked;
        await invitationRepository.UpdateAsync(invitation, ct);

        return Result.Success();
    }
}
