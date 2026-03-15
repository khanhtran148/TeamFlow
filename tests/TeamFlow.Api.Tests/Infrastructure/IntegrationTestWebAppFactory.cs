using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Tests.Common;

namespace TeamFlow.Api.Tests.Infrastructure;

/// <summary>
/// WebApplicationFactory configured for integration tests.
/// Replaces the real DB connection with the shared Testcontainer,
/// stubs out SignalR broadcast and RabbitMQ health check.
/// </summary>
public sealed class IntegrationTestWebAppFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:DefaultConnection", postgres.ConnectionString);
        builder.UseSetting("Jwt:Secret", TestJwtSettings.Secret);
        builder.UseSetting("Jwt:Issuer", TestJwtSettings.Issuer);
        builder.UseSetting("Jwt:Audience", TestJwtSettings.Audience);

        builder.ConfigureServices(services =>
        {
            // Replace IBroadcastService with no-op
            services.ReplaceService<IBroadcastService, NullBroadcastService>();

            // Replace RabbitMQ health check with always-healthy
            services.ReplaceHealthCheck();
        });
    }

    /// <summary>
    /// Ensures the database schema exists and seeds reference data.
    /// Called by ApiIntegrationTestBase during initialization.
    /// </summary>
    public async Task EnsureDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task SeedReferenceDataAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();
        await TestDataSeeder.SeedReferenceDataAsync(db, IntegrationTestBase.SeedOrgId, IntegrationTestBase.SeedUserId);
    }

}

internal sealed class AlwaysHealthyCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(HealthCheckResult.Healthy("Stubbed for integration tests"));
}
