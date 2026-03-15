using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.ProjectMemberships.GetMyPermissions;

public sealed class GetMyPermissionsHandler(
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser)
    : IRequestHandler<GetMyPermissionsQuery, Result<MyPermissionsDto>>
{
    public async Task<Result<MyPermissionsDto>> Handle(GetMyPermissionsQuery request, CancellationToken ct)
    {
        var role = await permissionChecker.GetEffectiveRoleAsync(currentUser.Id, request.ProjectId, ct);

        if (role is null)
            return Result.Success(new MyPermissionsDto(null, []));

        var permissions = PermissionMatrix.GetPermissions(role.Value)
            .Select(p => p.ToString())
            .ToArray();

        return Result.Success(new MyPermissionsDto(role.Value.ToString(), permissions));
    }
}
