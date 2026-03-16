using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Admin.ListOrganizations;

public sealed class AdminListOrganizationsHandler(
    IOrganizationRepository organizationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<AdminListOrganizationsQuery, Result<IEnumerable<AdminOrganizationDto>>>
{
    public async Task<Result<IEnumerable<AdminOrganizationDto>>> Handle(
        AdminListOrganizationsQuery request, CancellationToken ct)
    {
        if (currentUser.SystemRole != SystemRole.SystemAdmin)
            return DomainError.Forbidden<IEnumerable<AdminOrganizationDto>>("Access forbidden.");

        var orgs = await organizationRepository.ListAllAsync(ct);

        var dtos = orgs.Select(o => new AdminOrganizationDto(
            o.Id,
            o.Name,
            o.CreatedAt));

        return Result.Success(dtos);
    }
}
