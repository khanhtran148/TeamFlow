using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TeamFlow.Application;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Infrastructure.Repositories;
using TeamFlow.Infrastructure.Services;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Tests.Common;

public abstract class ApplicationTestBase : IAsyncLifetime
{
    private readonly PostgresCollectionFixture _fixture;
    private ServiceProvider? _provider;
    private IServiceScope? _scope;
    private IDbContextTransaction? _transaction;

    public static readonly Guid SeedOrgId = PostgresCollectionFixture.SeedOrgId;
    public static readonly Guid SeedUserId = PostgresCollectionFixture.SeedUserId;

    protected ISender Sender => _scope!.ServiceProvider.GetRequiredService<ISender>();
    protected TeamFlowDbContext DbContext => _scope!.ServiceProvider.GetRequiredService<TeamFlowDbContext>();

    protected ApplicationTestBase(PostgresCollectionFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Warning));

        services.AddDbContext<TeamFlowDbContext>(options =>
            options.UseNpgsql(_fixture.ConnectionString, npgsql =>
                npgsql.MigrationsAssembly("TeamFlow.Infrastructure")));

        // MediatR + validators + pipeline behaviors
        services.AddApplication();

        // ALL repositories
        services.AddScoped<IWorkItemRepository, WorkItemRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IReleaseRepository, ReleaseRepository>();
        services.AddScoped<IWorkItemLinkRepository, WorkItemLinkRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IProjectMembershipRepository, ProjectMembershipRepository>();
        services.AddScoped<IWorkItemHistoryRepository, WorkItemHistoryRepository>();
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<IBurndownDataPointRepository, BurndownDataPointRepository>();
        services.AddScoped<ISprintSnapshotRepository, SprintSnapshotRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IRetroSessionRepository, RetroSessionRepository>();
        services.AddScoped<IPlanningPokerSessionRepository, PlanningPokerSessionRepository>();
        services.AddScoped<IInAppNotificationRepository, InAppNotificationRepository>();
        services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<ISavedFilterRepository, SavedFilterRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<IEmailOutboxRepository, EmailOutboxRepository>();
        services.AddScoped<ISprintReportRepository, SprintReportRepository>();
        services.AddScoped<ITeamHealthSummaryRepository, TeamHealthSummaryRepository>();

        // Test stubs for non-DB services
        services.AddScoped<ICurrentUser, TestCurrentUser>();
        services.AddScoped<IPermissionChecker, AlwaysAllowTestPermissionChecker>();
        services.AddScoped<IHistoryService, HistoryService>();
        services.AddScoped<IBroadcastService, NullBroadcastService>();

        // Allow per-test-class customization
        ConfigureServices(services);

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _transaction = await DbContext.Database.BeginTransactionAsync();
    }

    protected virtual void ConfigureServices(IServiceCollection services) { }

    /// <summary>Helper to seed a project for the test.</summary>
    protected async Task<Project> SeedProjectAsync(Action<ProjectBuilder>? configure = null)
    {
        var builder = ProjectBuilder.New().WithOrganization(SeedOrgId);
        configure?.Invoke(builder);
        var project = builder.Build();
        DbContext.Projects.Add(project);
        await DbContext.SaveChangesAsync();
        return project;
    }

    /// <summary>Helper to seed a work item in a project.</summary>
    protected async Task<WorkItem> SeedWorkItemAsync(Guid projectId, Action<WorkItemBuilder>? configure = null)
    {
        var builder = WorkItemBuilder.New().WithProject(projectId);
        configure?.Invoke(builder);
        var item = builder.Build();
        DbContext.WorkItems.Add(item);
        await DbContext.SaveChangesAsync();
        return item;
    }

    public async Task DisposeAsync()
    {
        if (_transaction is not null) await _transaction.RollbackAsync();
        _scope?.Dispose();
        if (_provider is not null) await _provider.DisposeAsync();
    }
}
