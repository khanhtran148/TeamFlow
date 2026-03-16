using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Admin.ListOrganizations;

public sealed record AdminListOrganizationsQuery
    : IRequest<Result<IEnumerable<AdminOrganizationDto>>>;
