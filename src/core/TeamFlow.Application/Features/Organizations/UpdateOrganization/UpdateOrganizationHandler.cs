using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Organizations.UpdateOrganization;

public sealed class UpdateOrganizationHandler(
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateOrganizationCommand, Result<OrganizationDto>>
{
    public async Task<Result<OrganizationDto>> Handle(UpdateOrganizationCommand request, CancellationToken ct)
    {
        var org = await organizationRepository.GetByIdAsync(request.OrgId, ct);
        if (org is null)
            return DomainError.NotFound<OrganizationDto>("Organization not found.");

        // Check org membership — only Owner or Admin can update
        var role = await memberRepository.GetMemberRoleAsync(request.OrgId, currentUser.Id, ct);
        if (role is null || role == OrgRole.Member)
            return DomainError.Forbidden<OrganizationDto>("Only Org Owner or Admin can update this organization.");

        // Check slug uniqueness only if slug is changing
        if (org.Slug != request.Slug)
        {
            var slugExists = await organizationRepository.ExistsBySlugAsync(request.Slug, ct);
            if (slugExists)
                return DomainError.Conflict<OrganizationDto>("Slug already exists.");
        }

        org.Name = request.Name;
        org.Slug = request.Slug;

        await organizationRepository.UpdateAsync(org, ct);

        return Result.Success(new OrganizationDto(
            org.Id,
            org.Name,
            org.Slug,
            org.CreatedAt));
    }
}
