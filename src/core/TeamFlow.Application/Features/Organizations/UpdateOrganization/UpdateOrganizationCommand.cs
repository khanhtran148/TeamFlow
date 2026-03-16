using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Organizations;

namespace TeamFlow.Application.Features.Organizations.UpdateOrganization;

public sealed record UpdateOrganizationCommand(
    Guid OrgId,
    string Name,
    string Slug
) : IRequest<Result<OrganizationDto>>;
