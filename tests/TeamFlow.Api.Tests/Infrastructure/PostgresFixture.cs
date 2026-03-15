using Testcontainers.PostgreSql;

namespace TeamFlow.Api.Tests.Infrastructure;

/// <summary>
/// Shared Testcontainer PostgreSQL instance and WebApplicationFactory for the entire
/// "Integration" test collection. Starts once, shared across all test classes
/// decorated with [Collection("Integration")].
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("teamflow_integration")
        .WithUsername("teamflow_test")
        .WithPassword("teamflow_test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Shared WebApplicationFactory built once per collection.
    /// Initialized after the container starts.
    /// </summary>
    public IntegrationTestWebAppFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        Factory = new IntegrationTestWebAppFactory(this);
        _ = Factory.Server; // Force host build
        await Factory.EnsureDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _container.DisposeAsync().AsTask();
    }
}

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<PostgresFixture>;
