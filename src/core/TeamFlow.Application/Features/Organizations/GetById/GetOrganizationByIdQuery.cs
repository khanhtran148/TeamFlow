using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Organizations.GetById;

public sealed record GetOrganizationByIdQuery(Guid Id) : IRequest<Result<OrganizationDto>>;
