using Testcontainers.PostgreSql;

namespace TeamFlow.Api.Tests.Infrastructure;

/// <summary>
/// Shared Testcontainer PostgreSQL instance for the entire "Integration" test collection.
/// Starts once, shared across all test classes decorated with [Collection("Integration")].
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("teamflow_integration")
        .WithUsername("teamflow_test")
        .WithPassword("teamflow_test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync().AsTask();
    }
}

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<PostgresFixture>;
