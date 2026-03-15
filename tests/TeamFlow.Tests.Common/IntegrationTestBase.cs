using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Tests.Common;

/// <summary>
/// Base class for integration tests using Testcontainers to spin up a real PostgreSQL instance.
/// Every test class gets its own container, schema, and seeded Organization.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("teamflow_test")
        .WithUsername("teamflow_test")
        .WithPassword("teamflow_test")
        .Build();

    /// <summary>A seeded Organization ID available to all test subclasses.</summary>
    public static readonly Guid SeedOrgId = new("00000000-0000-0000-0000-000000000010");

    /// <summary>A seeded User ID that matches TestCurrentUser.Id used in test stubs.</summary>
    public static readonly Guid SeedUserId = new("00000000-0000-0000-0000-000000000001");

    protected IServiceProvider Services { get; private set; } = null!;
    protected TeamFlowDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var services = new ServiceCollection();

        services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Warning));

        services.AddDbContext<TeamFlowDbContext>(options =>
        {
            options.UseNpgsql(_postgres.GetConnectionString(), npgsql =>
            {
                npgsql.MigrationsAssembly("TeamFlow.Infrastructure");
            });
        });

        await ConfigureServices(services);

        Services = services.BuildServiceProvider();
        DbContext = Services.GetRequiredService<TeamFlowDbContext>();

        await DbContext.Database.EnsureCreatedAsync();

        // Seed required reference data
        await SeedReferenceDataAsync(DbContext);
    }

    private static async Task SeedReferenceDataAsync(TeamFlowDbContext ctx)
    {
        // Seed Organization so projects can be created without FK violations
        if (!await ctx.Set<Organization>().AnyAsync(o => o.Id == SeedOrgId))
        {
            var org = new Organization { Name = "Test Org" };
            ctx.Entry(org).Property(nameof(Organization.Id)).CurrentValue = SeedOrgId;
            ctx.Set<Organization>().Add(org);
        }

        // Seed User so assignee FK checks pass
        if (!await ctx.Set<User>().AnyAsync(u => u.Id == SeedUserId))
        {
            var user = new User { Email = "test@teamflow.dev", Name = "Test User", PasswordHash = "not-a-real-hash" };
            ctx.Entry(user).Property(nameof(User.Id)).CurrentValue = SeedUserId;
            ctx.Set<User>().Add(user);
        }

        await ctx.SaveChangesAsync();
    }

    /// <summary>Override to add additional services for specific test classes.</summary>
    protected virtual Task ConfigureServices(IServiceCollection services)
        => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
