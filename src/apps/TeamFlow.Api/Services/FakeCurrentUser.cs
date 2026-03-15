using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Api.Services;

/// <summary>
/// Phase 1 stub: returns a fixed seed-user ID. Replace with JWT-based implementation in Phase 2.
/// </summary>
public sealed class FakeCurrentUser : ICurrentUser
{
    // Fixed seed-user ID used for all Phase 1 operations.
    public static readonly Guid SeedUserId = new("00000000-0000-0000-0000-000000000001");

    public Guid Id => SeedUserId;
    public string Email => "seed@teamflow.dev";
    public string Name => "Seed User";
    public bool IsAuthenticated => true;
}
