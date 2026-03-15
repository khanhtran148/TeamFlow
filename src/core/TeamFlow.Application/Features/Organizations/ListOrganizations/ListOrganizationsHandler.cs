using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Organizations.ListOrganizations;

public sealed class ListOrganizationsHandler(
    IOrganizationRepository organizationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ListOrganizationsQuery, Result<IEnumerable<OrganizationDto>>>
{
    public async Task<Result<IEnumerable<OrganizationDto>>> Handle(
        ListOrganizationsQuery request, CancellationToken ct)
    {
        var organizations = await organizationRepository.ListByUserAsync(currentUser.Id, ct);

        var dtos = organizations.Select(o => new OrganizationDto(
            o.Id,
            o.Name,
            o.CreatedAt));

        return Result.Success(dtos);
    }
}
