using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Organizations.CreateOrganization;

public sealed record CreateOrganizationCommand(
    string Name
) : IRequest<Result<OrganizationDto>>;
