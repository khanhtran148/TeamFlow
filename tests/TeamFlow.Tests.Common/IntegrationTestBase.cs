using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using TeamFlow.Application;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Infrastructure.Repositories;
using TeamFlow.Infrastructure.Services;

namespace TeamFlow.Tests.Common;

/// <summary>
/// Base class for integration tests using Testcontainers to spin up a real PostgreSQL instance.
/// Every test class gets its own container, schema, and seeded Organization.
/// Registers all common Application + Infrastructure services. Override ConfigureServices for extras.
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

        RegisterCommonServices(services);
        await ConfigureServices(services);

        Services = services.BuildServiceProvider();
        DbContext = Services.GetRequiredService<TeamFlowDbContext>();

        await DbContext.Database.EnsureCreatedAsync();
        await SeedReferenceDataAsync(DbContext);
    }

    private static void RegisterCommonServices(IServiceCollection services)
    {
        services.AddApplication();

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IReleaseRepository, ReleaseRepository>();
        services.AddScoped<IWorkItemRepository, WorkItemRepository>();
        services.AddScoped<IWorkItemLinkRepository, WorkItemLinkRepository>();
        services.AddScoped<IProjectMembershipRepository, ProjectMembershipRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();

        services.AddScoped<ICurrentUser, TestCurrentUser>();
        services.AddScoped<IPermissionChecker, AlwaysAllowTestPermissionChecker>();
        services.AddScoped<IHistoryService, HistoryService>();
        services.AddScoped<IBroadcastService, NullBroadcastService>();
    }

    private static Task SeedReferenceDataAsync(TeamFlowDbContext ctx)
        => TestDataSeeder.SeedReferenceDataAsync(ctx, SeedOrgId, SeedUserId);

    /// <summary>Override to add or replace services for specific test classes.</summary>
    protected virtual Task ConfigureServices(IServiceCollection services)
        => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
