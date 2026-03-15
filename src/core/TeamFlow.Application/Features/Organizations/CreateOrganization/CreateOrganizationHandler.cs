using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Organizations.CreateOrganization;

public sealed class CreateOrganizationHandler(
    IOrganizationRepository organizationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<CreateOrganizationCommand, Result<OrganizationDto>>
{
    public async Task<Result<OrganizationDto>> Handle(CreateOrganizationCommand request, CancellationToken ct)
    {
        var organization = new Organization
        {
            Name = request.Name,
            CreatedByUserId = currentUser.Id
        };

        await organizationRepository.AddAsync(organization, ct);

        return Result.Success(new OrganizationDto(
            organization.Id,
            organization.Name,
            organization.CreatedAt));
    }
}
