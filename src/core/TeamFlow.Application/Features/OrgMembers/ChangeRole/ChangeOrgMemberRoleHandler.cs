using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.OrgMembers.ChangeRole;

public sealed class ChangeOrgMemberRoleHandler(
    IOrganizationMemberRepository memberRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ChangeOrgMemberRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeOrgMemberRoleCommand request, CancellationToken ct)
    {
        // 1. Cannot change your own role
        if (request.UserId == currentUser.Id)
            return DomainError.Validation("You cannot change your own role.");

        // 2. Permission check — Owner or Admin only
        var currentUserRole = await memberRepository.GetMemberRoleAsync(request.OrgId, currentUser.Id, ct);
        if (currentUserRole is null || currentUserRole == OrgRole.Member)
            return DomainError.Forbidden("Only Org Owner or Admin can change member roles.");

        // 3. Admin cannot promote to Owner
        if (currentUserRole == OrgRole.Admin && request.NewRole == OrgRole.Owner)
            return DomainError.Forbidden(
                "Only an Owner can promote another member to Owner.");

        // 4. Look up target member
        var targetMember = await memberRepository.GetByOrgAndUserAsync(request.OrgId, request.UserId, ct);
        if (targetMember is null)
            return DomainError.NotFound("Member not found in this organization.");

        // 5. Cannot demote last Owner
        if (targetMember.Role == OrgRole.Owner && request.NewRole != OrgRole.Owner)
        {
            var ownerCount = await memberRepository.CountByRoleAsync(request.OrgId, OrgRole.Owner, ct);
            if (ownerCount <= 1)
                return DomainError.Validation(
                    "Cannot demote the last Owner. Assign another Owner first.");
        }

        // 6. Apply role change and persist
        targetMember.Role = request.NewRole;
        await memberRepository.UpdateAsync(targetMember, ct);

        return Result.Success();
    }
}
