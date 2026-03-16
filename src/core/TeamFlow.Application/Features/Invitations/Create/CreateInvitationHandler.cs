using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Invitations.Create;

public sealed class CreateInvitationHandler(
    IInvitationRepository invitationRepository,
    IOrganizationMemberRepository memberRepository,
    IOrganizationRepository organizationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<CreateInvitationCommand, Result<CreateInvitationResponse>>
{
    public async Task<Result<CreateInvitationResponse>> Handle(
        CreateInvitationCommand request, CancellationToken ct)
    {
        // 1. Verify org exists
        var org = await organizationRepository.GetByIdAsync(request.OrgId, ct);
        if (org is null)
            return DomainError.NotFound<CreateInvitationResponse>("Organization not found.");

        // 2. Permission check — Owner or Admin only
        var role = await memberRepository.GetMemberRoleAsync(request.OrgId, currentUser.Id, ct);
        if (role is null || role == OrgRole.Member)
            return DomainError.Forbidden<CreateInvitationResponse>(
                "Only Org Owner or Admin can create invitations.");

        // 3. Cannot invite as Owner
        if (request.Role == OrgRole.Owner)
            return DomainError.Forbidden<CreateInvitationResponse>(
                "Cannot invite a user as Owner. Owner role can only be transferred.");

        // 4. Generate cryptographically random token
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        // 5. Hash for storage
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)))
            .ToLowerInvariant();

        // 6. Persist invitation
        var invitation = new Invitation
        {
            OrganizationId = request.OrgId,
            InvitedByUserId = currentUser.Id,
            Email = request.Email,
            Role = request.Role,
            TokenHash = tokenHash,
            Status = InviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await invitationRepository.AddAsync(invitation, ct);

        // 7. Return raw token only on creation (caller constructs the URL)
        return Result.Success(new CreateInvitationResponse(
            invitation.Id,
            rawToken,
            invitation.Role,
            invitation.ExpiresAt,
            invitation.Status));
    }
}
