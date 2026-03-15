using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Tests.Infrastructure;

/// <summary>
/// Base class for integration tests using Testcontainers to spin up a real PostgreSQL instance.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("teamflow_test")
        .WithUsername("teamflow_test")
        .WithPassword("teamflow_test")
        .Build();

    protected IServiceProvider Services { get; private set; } = null!;
    protected TeamFlowDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var services = new ServiceCollection();

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

        // Apply migrations (EnsureCreated for Phase 0, migrations once they exist)
        await DbContext.Database.EnsureCreatedAsync();
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
