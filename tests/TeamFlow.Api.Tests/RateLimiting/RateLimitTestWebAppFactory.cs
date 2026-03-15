using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TeamFlow.Api.RateLimiting;
using TeamFlow.Api.Tests.Infrastructure;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Tests.Common;

namespace TeamFlow.Api.Tests.RateLimiting;

/// <summary>
/// WebApplicationFactory with low rate limits for testing 429 responses.
/// Overrides WritePermitLimit=3, GeneralPermitLimit=3, Window=60s.
/// </summary>
internal sealed class RateLimitTestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly PostgresFixture _postgres;

    public RateLimitTestWebAppFactory(PostgresFixture postgres)
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

        // Override rate limit settings to low values for testability
        builder.UseSetting("RateLimiting:WritePermitLimit", "3");
        builder.UseSetting("RateLimiting:WriteWindowSeconds", "60");
        builder.UseSetting("RateLimiting:GeneralPermitLimit", "3");
        builder.UseSetting("RateLimiting:GeneralWindowSeconds", "60");
        builder.UseSetting("RateLimiting:SegmentsPerWindow", "1");

        builder.ConfigureServices(services =>
        {
            ReplaceService<IBroadcastService, NullBroadcastService>(services);
            ReplaceHealthCheck(services);
        });
    }

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
            var org = new Organization { Name = "Rate Limit Test Org" };
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

internal sealed class NullBroadcastServiceForRateLimit : IBroadcastService
{
    public Task BroadcastToProjectAsync(Guid projectId, string eventName, object payload, CancellationToken ct = default) => Task.CompletedTask;
    public Task BroadcastToSprintAsync(Guid sprintId, string eventName, object payload, CancellationToken ct = default) => Task.CompletedTask;
    public Task BroadcastToUserAsync(Guid userId, string eventName, object payload, CancellationToken ct = default) => Task.CompletedTask;
    public Task BroadcastToRetroSessionAsync(Guid sessionId, string eventName, object payload, CancellationToken ct = default) => Task.CompletedTask;
}
