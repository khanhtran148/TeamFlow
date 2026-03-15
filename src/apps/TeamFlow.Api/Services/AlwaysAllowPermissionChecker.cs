using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Services;

/// <summary>
/// Phase 1 stub: always returns true for all permission checks. Replace in Phase 2 with real RBAC.
/// </summary>
public sealed class AlwaysAllowPermissionChecker : IPermissionChecker
{
    public Task<bool> HasPermissionAsync(
        Guid userId,
        Guid projectId,
        Permission permission,
        CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<ProjectRole?> GetEffectiveRoleAsync(
        Guid userId,
        Guid projectId,
        CancellationToken ct = default)
        => Task.FromResult<ProjectRole?>(ProjectRole.Developer);
}
