using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Organizations;

namespace TeamFlow.Application.Features.Organizations.ListMyOrganizations;

public sealed record ListMyOrganizationsQuery() : IRequest<Result<IEnumerable<MyOrganizationDto>>>;
