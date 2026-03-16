using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Tests.Common;

public sealed class PostgresCollectionFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("teamflow_test")
        .WithUsername("teamflow_test")
        .WithPassword("teamflow_test")
        .Build();

    public static readonly Guid SeedOrgId = new("00000000-0000-0000-0000-000000000010");
    public static readonly Guid SeedUserId = new("00000000-0000-0000-0000-000000000001");

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var services = new ServiceCollection();
        services.AddDbContext<TeamFlowDbContext>(options =>
            options.UseNpgsql(ConnectionString, npgsql =>
                npgsql.MigrationsAssembly("TeamFlow.Infrastructure")));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();
        await db.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedReferenceDataAsync(db, SeedOrgId, SeedUserId);
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
