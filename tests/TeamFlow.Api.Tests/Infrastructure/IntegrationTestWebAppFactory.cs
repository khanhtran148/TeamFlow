using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Tests.Common;

namespace TeamFlow.Api.Tests.Infrastructure;

/// <summary>
/// WebApplicationFactory configured for integration tests.
/// Replaces the real DB connection with the shared Testcontainer,
/// stubs out SignalR broadcast and RabbitMQ health check.
/// </summary>
public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly PostgresFixture _postgres;

    public IntegrationTestWebAppFactory(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.ConnectionString);
        builder.UseSetting("Jwt:Secret", TestJwtSettings.Secret);
        builder.UseSetting("Jwt:Issuer", TestJwtSettings.Issuer);
        builder.UseSetting("Jwt:Audience", TestJwtSettings.Audience);

        builder.ConfigureServices(services =>
        {
            // Replace IBroadcastService with no-op
            ReplaceService<IBroadcastService, NullBroadcastService>(services);

            // Replace RabbitMQ health check with always-healthy
            ReplaceHealthCheck(services);
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

        if (!await db.Set<Organization>().AnyAsync(o => o.Id == IntegrationTestBase.SeedOrgId))
        {
            var org = new Organization { Name = "Integration Test Org" };
            db.Entry(org).Property(nameof(Organization.Id)).CurrentValue = IntegrationTestBase.SeedOrgId;
            db.Set<Organization>().Add(org);
        }

        if (!await db.Set<User>().AnyAsync(u => u.Id == IntegrationTestBase.SeedUserId))
        {
            var user = new User
            {
                Email = "test@teamflow.dev",
                Name = "Test User",
                PasswordHash = "not-a-real-hash"
            };
            db.Entry(user).Property(nameof(User.Id)).CurrentValue = IntegrationTestBase.SeedUserId;
            db.Set<User>().Add(user);
        }

        await db.SaveChangesAsync();
    }

    private static void ReplaceService<TInterface, TImplementation>(IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TInterface));
        if (descriptor is not null)
            services.Remove(descriptor);

        services.AddScoped<TInterface, TImplementation>();
    }

    private static void ReplaceHealthCheck(IServiceCollection services)
    {
        // Remove existing RabbitMQ health check registration and add a no-op one
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IHealthCheck) &&
            d.ImplementationType?.Name == "RabbitMqHealthCheck");
        if (descriptor is not null)
            services.Remove(descriptor);

        // Configure health checks to replace rabbitmq with always-healthy
        services.Configure<HealthCheckServiceOptions>(options =>
        {
            var rabbitCheck = options.Registrations.FirstOrDefault(r => r.Name == "rabbitmq");
            if (rabbitCheck is not null)
            {
                options.Registrations.Remove(rabbitCheck);
                options.Registrations.Add(new HealthCheckRegistration(
                    "rabbitmq",
                    _ => new AlwaysHealthyCheck(),
                    failureStatus: HealthStatus.Degraded,
                    tags: ["ready"]));
            }
        });
    }
}

internal sealed class AlwaysHealthyCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(HealthCheckResult.Healthy("Stubbed for integration tests"));
}
