using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Organizations.GetById;

public sealed class GetOrganizationByIdHandler(
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetOrganizationByIdQuery, Result<OrganizationDto>>
{
    public async Task<Result<OrganizationDto>> Handle(GetOrganizationByIdQuery request, CancellationToken ct)
    {
        var org = await organizationRepository.GetByIdAsync(request.Id, ct);
        if (org is null)
            return DomainError.NotFound<OrganizationDto>("Organization not found.");

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
