using Microsoft.AspNetCore.Mvc.Testing;
using TeamFlow.Api.Tests.Infrastructure;

namespace TeamFlow.Api.Tests.RateLimiting;

/// <summary>
/// Base class for rate-limiting integration tests.
/// Extends ApiIntegrationTestBase but overrides the factory with a custom
/// RateLimitTestWebAppFactory that uses low rate limit settings
/// so tests can trigger 429 responses without sending dozens of requests.
/// </summary>
[Collection("Integration")]
public abstract class RateLimitTestBase : ApiIntegrationTestBase
{
    private RateLimitTestWebAppFactory? _rateLimitFactory;
    private readonly PostgresFixture _postgres;

    protected override WebApplicationFactory<Program> Factory =>
        _rateLimitFactory ?? throw new InvalidOperationException("Factory not initialized. Call InitializeAsync first.");

    protected RateLimitTestBase(PostgresFixture postgres) : base(postgres)
    {
        _postgres = postgres;
    }

    public override async Task InitializeAsync()
    {
        _rateLimitFactory = new RateLimitTestWebAppFactory(_postgres);
        _ = _rateLimitFactory.Server;
        await _rateLimitFactory.EnsureDatabaseAsync();

        await base.InitializeAsync();
    }

    public override async Task DisposeAsync()
    {
        if (_rateLimitFactory is not null)
            await _rateLimitFactory.DisposeAsync();

        await base.DisposeAsync();
    }
}
