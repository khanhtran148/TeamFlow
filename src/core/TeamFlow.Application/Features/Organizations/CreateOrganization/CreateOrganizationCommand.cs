using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Organizations;

namespace TeamFlow.Application.Features.Organizations.CreateOrganization;

public sealed record CreateOrganizationCommand(
    string Name,
    string? Slug
) : IRequest<Result<OrganizationDto>>;
