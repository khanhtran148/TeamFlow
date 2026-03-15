using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.ProjectMemberships.GetMyPermissions;

public sealed record GetMyPermissionsQuery(Guid ProjectId) : IRequest<Result<MyPermissionsDto>>;

public sealed record MyPermissionsDto(
    string? Role,
    string[] Permissions);
