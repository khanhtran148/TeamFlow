using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.BackgroundServices.Services;

/// <summary>
/// ICurrentUser implementation for background services where there is no HTTP context.
/// Represents the system/service account identity.
/// </summary>
public sealed class SystemCurrentUser : ICurrentUser
{
    public Guid Id => Guid.Empty;
    public string Email => "system@teamflow.local";
    public string Name => "System";
    public bool IsAuthenticated => true;
    public SystemRole SystemRole => SystemRole.SystemAdmin;
}
