using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Admin.UpdateOrganization;

public sealed record AdminUpdateOrgCommand(
    Guid OrgId,
    string Name,
    string Slug
) : IRequest<Result>;
