using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Admin.ChangeOrgStatus;

public sealed class AdminChangeOrgStatusHandler(
    IOrganizationRepository organizationRepository,
    IInvitationRepository invitationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<AdminChangeOrgStatusCommand, Result>
{
    public async Task<Result> Handle(AdminChangeOrgStatusCommand request, CancellationToken ct)
    {
        if (currentUser.SystemRole != SystemRole.SystemAdmin)
            return DomainError.Forbidden("Access forbidden.");

        var org = await organizationRepository.GetByIdAsync(request.OrgId, ct);
        if (org is null)
            return DomainError.NotFound("Organization not found.");

        org.IsActive = request.IsActive;
        await organizationRepository.UpdateAsync(org, ct);

        if (!request.IsActive)
            await invitationRepository.RevokePendingByOrgAsync(org.Id, ct);

        return Result.Success();
    }
}
