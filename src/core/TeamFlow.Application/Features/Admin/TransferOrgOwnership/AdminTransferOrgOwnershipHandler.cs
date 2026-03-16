using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Admin.TransferOrgOwnership;

public sealed class AdminTransferOrgOwnershipHandler(
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser)
    : IRequestHandler<AdminTransferOrgOwnershipCommand, Result>
{
    public async Task<Result> Handle(AdminTransferOrgOwnershipCommand request, CancellationToken ct)
    {
        if (currentUser.SystemRole != SystemRole.SystemAdmin)
            return DomainError.Forbidden("Access forbidden.");

        var org = await organizationRepository.GetByIdAsync(request.OrgId, ct);
        if (org is null)
            return DomainError.NotFound("Organization not found.");

        var newOwnerUser = await userRepository.GetByIdAsync(request.NewOwnerUserId, ct);
        if (newOwnerUser is null)
            return DomainError.NotFound("User not found.");

        var newOwnerMembership = await memberRepository.GetByOrgAndUserAsync(
            request.OrgId, request.NewOwnerUserId, ct);
        if (newOwnerMembership is null)
            return DomainError.Validation("The specified user is not a member of this organization.");

        if (newOwnerMembership.Role == OrgRole.Owner)
            return DomainError.Validation("The specified user is already the owner of this organization.");

        // Find current owner
        var allMembers = await memberRepository.ListByOrgWithUsersAsync(request.OrgId, ct);
        var currentOwnerMembership = allMembers
            .Select(m => m.Member)
            .FirstOrDefault(m => m.Role == OrgRole.Owner && m.UserId != request.NewOwnerUserId);

        // Promote new owner
        newOwnerMembership.Role = OrgRole.Owner;
        await memberRepository.UpdateAsync(newOwnerMembership, ct);

        // Demote current owner (if exists)
        if (currentOwnerMembership is not null)
        {
            currentOwnerMembership.Role = OrgRole.Admin;
            await memberRepository.UpdateAsync(currentOwnerMembership, ct);
        }

        return Result.Success();
    }
}
