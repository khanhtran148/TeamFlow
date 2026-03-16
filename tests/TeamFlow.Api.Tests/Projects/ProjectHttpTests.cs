using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Api.Tests.Infrastructure;
using TeamFlow.Application.Features.Projects;
using TeamFlow.Application.Features.Projects.CreateProject;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Tests.Projects;

[Collection("Integration")]
public sealed class ProjectHttpTests(PostgresFixture postgres) : ApiIntegrationTestBase(postgres)
{
    [Fact]
    public async Task Create_WithValidBody_Returns201()
    {
        // SeedUserId must be an Org Owner/Admin to create projects
        await SeedOrgMemberAsync(SeedUserId, SeedOrgId, OrgRole.Owner);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var response = await client.PostAsJsonAsync(
            "/api/v1/projects",
            new CreateProjectCommand(SeedOrgId, "HTTP Test Project", "Created via HTTP"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var project = await response.Content.ReadFromJsonAsync<ProjectDto>();
        project.Should().NotBeNull();
        project!.Name.Should().Be("HTTP Test Project");
        project.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        var client = CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/projects",
            new CreateProjectCommand(SeedOrgId, "No Auth Project", null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_AfterCreate_Returns200WithProject()
    {
        // SeedUserId must be an Org Owner/Admin to create projects
        await SeedOrgMemberAsync(SeedUserId, SeedOrgId, OrgRole.Owner);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/projects",
            new CreateProjectCommand(SeedOrgId, "Fetch Me Project", "Description"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();

        var getResponse = await client.GetAsync($"/api/v1/projects/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ProjectDto>();
        fetched!.Name.Should().Be("Fetch Me Project");
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var client = CreateAuthenticatedClient(ProjectRole.Developer);

        var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var client = CreateAnonymousClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
