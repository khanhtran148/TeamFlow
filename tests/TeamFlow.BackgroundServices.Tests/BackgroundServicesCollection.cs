using TeamFlow.Tests.Common;

namespace TeamFlow.BackgroundServices.Tests;

[CollectionDefinition("BackgroundServices")]
public sealed class BackgroundServicesCollection : ICollectionFixture<PostgresCollectionFixture>;
