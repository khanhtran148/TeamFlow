using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Organizations.CreateOrganization;

public sealed class CreateOrganizationHandler(
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    ICurrentUser currentUser)
    : IRequestHandler<CreateOrganizationCommand, Result<OrganizationDto>>
{
    public async Task<Result<OrganizationDto>> Handle(CreateOrganizationCommand request, CancellationToken ct)
    {
        // Only SystemAdmin can create organizations
        if (currentUser.SystemRole != SystemRole.SystemAdmin)
            return DomainError.Forbidden<OrganizationDto>("Only SystemAdmin can create organizations.");

        // Generate or use provided slug
        var slug = !string.IsNullOrWhiteSpace(request.Slug)
            ? request.Slug
            : Organization.GenerateSlug(request.Name);

        var organization = new Organization
        {
            Name = request.Name,
            Slug = slug,
            CreatedByUserId = currentUser.Id
        };

        await organizationRepository.AddAsync(organization, ct);

        // Create Owner membership for the creator
        var ownerMember = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = currentUser.Id,
            Role = OrgRole.Owner
        };
        await memberRepository.AddAsync(ownerMember, ct);

        return Result.Success(new OrganizationDto(
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.CreatedAt));
    }
}
