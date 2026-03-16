using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Organizations;

namespace TeamFlow.Application.Features.Organizations.GetBySlug;

public sealed record GetOrganizationBySlugQuery(string Slug) : IRequest<Result<OrganizationDto>>;
