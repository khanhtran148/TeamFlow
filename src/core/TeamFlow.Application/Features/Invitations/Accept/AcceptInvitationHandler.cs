using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Invitations.Accept;

public sealed class AcceptInvitationHandler(
    IInvitationRepository invitationRepository,
    IOrganizationMemberRepository memberRepository,
    ICurrentUser currentUser)
    : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResponse>>
{
    public async Task<Result<AcceptInvitationResponse>> Handle(
        AcceptInvitationCommand request, CancellationToken ct)
    {
        // 1. Hash the raw token the same way it was hashed on creation
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)))
            .ToLowerInvariant();

        // 2. Look up by hash
        var invitation = await invitationRepository.GetByTokenHashAsync(tokenHash, ct);
        if (invitation is null)
            return DomainError.NotFound<AcceptInvitationResponse>("Invitation not found.");

        // 3. Check status
        if (invitation.Status == InviteStatus.Accepted)
            return DomainError.Validation<AcceptInvitationResponse>(
                "This invitation has already been accepted.");

        if (invitation.Status == InviteStatus.Revoked)
            return DomainError.Validation<AcceptInvitationResponse>(
                "This invitation has been revoked.");

        // 4. Check expiry
        if (invitation.ExpiresAt < DateTime.UtcNow)
            return DomainError.Validation<AcceptInvitationResponse>(
                "This invitation has expired.");

        // 5. Verify email matches (prevent token theft — any user with the token accepting)
        if (!string.IsNullOrEmpty(invitation.Email) &&
            !string.Equals(invitation.Email, currentUser.Email, StringComparison.OrdinalIgnoreCase))
            return DomainError.Forbidden<AcceptInvitationResponse>(
                "This invitation was sent to a different email address.");

        // 6. Check if user is already a member
        var alreadyMember = await memberRepository.IsMemberAsync(
            invitation.OrganizationId, currentUser.Id, ct);
        if (alreadyMember)
            return DomainError.Validation<AcceptInvitationResponse>(
                "You are already a member of this organization.");

        // 7. Create org membership
        var member = new OrganizationMember
        {
            OrganizationId = invitation.OrganizationId,
            UserId = currentUser.Id,
            Role = invitation.Role
        };
        await memberRepository.AddAsync(member, ct);

        // 8. Update invitation status
        invitation.Status = InviteStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;
        invitation.AcceptedByUserId = currentUser.Id;
        await invitationRepository.UpdateAsync(invitation, ct);

        var slug = invitation.Organization?.Slug ?? string.Empty;
        return Result.Success(new AcceptInvitationResponse(
            invitation.OrganizationId,
            slug,
            invitation.Role));
    }
}
