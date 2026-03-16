using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Organizations.GetBySlug;

public sealed class GetOrganizationBySlugHandler(
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetOrganizationBySlugQuery, Result<OrganizationDto>>
{
    public async Task<Result<OrganizationDto>> Handle(GetOrganizationBySlugQuery request, CancellationToken ct)
    {
        var org = await organizationRepository.GetBySlugAsync(request.Slug, ct);
        if (org is null)
            return DomainError.NotFound<OrganizationDto>("Organization not found.");

        // Only members can view the org
        var isMember = await memberRepository.IsMemberAsync(org.Id, currentUser.Id, ct);
        if (!isMember)
            return DomainError.Forbidden<OrganizationDto>("Access denied.");

        return Result.Success(new OrganizationDto(
            org.Id,
            org.Name,
            org.Slug,
            org.CreatedAt));
    }
}
