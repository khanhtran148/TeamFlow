using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Organizations.ListOrganizations;

public sealed record ListOrganizationsQuery : IRequest<Result<IEnumerable<OrganizationDto>>>;
