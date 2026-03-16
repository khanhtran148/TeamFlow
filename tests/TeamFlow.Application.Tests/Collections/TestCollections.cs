using TeamFlow.Tests.Common;

namespace TeamFlow.Application.Tests.Collections;

[CollectionDefinition("WorkItems")]
public sealed class WorkItemsCollection : ICollectionFixture<PostgresCollectionFixture>;

[CollectionDefinition("Projects")]
public sealed class ProjectsCollection : ICollectionFixture<PostgresCollectionFixture>;

[CollectionDefinition("Sprints")]
public sealed class SprintsCollection : ICollectionFixture<PostgresCollectionFixture>;

[CollectionDefinition("Releases")]
public sealed class ReleasesCollection : ICollectionFixture<PostgresCollectionFixture>;

[CollectionDefinition("Dashboard")]
public sealed class DashboardCollection : ICollectionFixture<PostgresCollectionFixture>;

[CollectionDefinition("Reports")]
public sealed class ReportsCollection : ICollectionFixture<PostgresCollectionFixture>;

[CollectionDefinition("Social")]
public sealed class SocialCollection : ICollectionFixture<PostgresCollectionFixture>;

[CollectionDefinition("Auth")]
public sealed class AuthCollection : ICollectionFixture<PostgresCollectionFixture>;
