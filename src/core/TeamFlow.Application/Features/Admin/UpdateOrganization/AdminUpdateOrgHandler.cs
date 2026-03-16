using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Admin.UpdateOrganization;

public sealed class AdminUpdateOrgHandler(
    IOrganizationRepository organizationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<AdminUpdateOrgCommand, Result>
{
    public async Task<Result> Handle(AdminUpdateOrgCommand request, CancellationToken ct)
    {
        if (currentUser.SystemRole != SystemRole.SystemAdmin)
            return DomainError.Forbidden("Access forbidden.");

        var org = await organizationRepository.GetByIdAsync(request.OrgId, ct);
        if (org is null)
            return DomainError.NotFound("Organization not found.");

        // Check slug uniqueness, excluding the current org
        if (org.Slug != request.Slug)
        {
            var slugTaken = await organizationRepository.ExistsBySlugAsync(request.Slug, ct);
            if (slugTaken)
                return DomainError.Conflict($"Slug '{request.Slug}' is already in use.");
        }

        org.Name = request.Name;
        org.Slug = request.Slug;
        await organizationRepository.UpdateAsync(org, ct);

        return Result.Success();
    }
}
