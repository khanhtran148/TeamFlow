using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.OrgMembers.Remove;

public sealed class RemoveOrgMemberHandler(
    IOrganizationMemberRepository memberRepository,
    ICurrentUser currentUser)
    : IRequestHandler<RemoveOrgMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveOrgMemberCommand request, CancellationToken ct)
    {
        // 1. Cannot remove yourself
        if (request.UserId == currentUser.Id)
            return DomainError.Validation("You cannot remove yourself from the organization.");

        // 2. Permission check — Owner or Admin only
        var currentUserRole = await memberRepository.GetMemberRoleAsync(request.OrgId, currentUser.Id, ct);
        if (currentUserRole is null || currentUserRole == OrgRole.Member)
            return DomainError.Forbidden("Only Org Owner or Admin can remove members.");

        // 3. Look up target member
        var targetMember = await memberRepository.GetByOrgAndUserAsync(request.OrgId, request.UserId, ct);
        if (targetMember is null)
            return DomainError.NotFound("Member not found in this organization.");

        // 4. Cannot remove last Owner
        if (targetMember.Role == OrgRole.Owner)
        {
            var ownerCount = await memberRepository.CountByRoleAsync(request.OrgId, OrgRole.Owner, ct);
            if (ownerCount <= 1)
                return DomainError.Validation(
                    "Cannot remove the last Owner. Assign another Owner first.");
        }

        // 5. Delete membership
        await memberRepository.DeleteAsync(targetMember, ct);

        return Result.Success();
    }
}
