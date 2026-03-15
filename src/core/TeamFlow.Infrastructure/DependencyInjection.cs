using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Infrastructure.Repositories;
using TeamFlow.Infrastructure.Services;

namespace TeamFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

        services.AddDbContext<TeamFlowDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName);
                npgsql.EnableRetryOnFailure(3);
            });
        });

        // Repositories
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

        // Services
        services.AddScoped<IHistoryService, HistoryService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();

        return services;
    }
}
