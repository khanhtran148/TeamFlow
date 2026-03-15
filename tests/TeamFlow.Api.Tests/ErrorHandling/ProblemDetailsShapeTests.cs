using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TeamFlow.Api.Tests.Infrastructure;
using TeamFlow.Application.Features.Sprints.CreateSprint;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Api.Tests.ErrorHandling;

/// <summary>
/// Verifies that error responses are structured as ProblemDetails (RFC 7807)
/// with the expected fields: status, title, detail, instance.
/// </summary>
[Collection("Integration")]
public sealed class ProblemDetailsShapeTests(PostgresFixture postgres) : ApiIntegrationTestBase(postgres)
{
    private const string SprintsUrl = "/api/v1/sprints";

    [Fact]
    public async Task BadRequest_ReturnsProblemDetailsWithRequiredFields()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        // Empty name triggers validation error -> 400
        var response = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertProblemDetailsShape(response, 400);
    }

    [Fact]
    public async Task NotFound_ReturnsProblemDetailsWithRequiredFields()
    {
        var projectId = await SeedProjectAsync(ProjectRole.Developer);
        var client = CreateAuthenticatedClient(ProjectRole.Developer);

        var response = await client.GetAsync($"{SprintsUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await AssertProblemDetailsShape(response, 404);
    }

    [Fact]
    public async Task Forbidden_ReturnsProblemDetailsWithRequiredFields()
    {
        var projectId = await SeedProjectAsync(ProjectRole.Viewer);
        var client = CreateAuthenticatedClient(ProjectRole.Viewer);

        var response = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Sprint X", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AssertProblemDetailsShape(response, 403);
    }

    [Fact]
    public async Task Conflict_ReturnsProblemDetailsWithRequiredFields()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        // Create two sprints, add items & dates, start both to get conflict
        var sprintId1 = await SeedSprintWithItemAndDatesAsync(projectId);
        var sprintId2 = await SeedSprintWithItemAndDatesAsync(projectId, "Sprint 2");

        // Start first sprint
        await client.PostAsync($"{SprintsUrl}/{sprintId1}/start", null);

        // Try to start second sprint - conflict because one is already active
        var response = await client.PostAsync($"{SprintsUrl}/{sprintId2}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await AssertProblemDetailsShape(response, 409);
    }

    [Fact]
    public async Task ErrorResponse_HasApplicationProblemJsonContentType()
    {
        var projectId = await SeedProjectAsync(ProjectRole.Developer);
        var client = CreateAuthenticatedClient(ProjectRole.Developer);

        var response = await client.GetAsync($"{SprintsUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        // ASP.NET Core returns either application/problem+json or application/json
        contentType.Should().BeOneOf("application/problem+json", "application/json");
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private static async Task AssertProblemDetailsShape(HttpResponseMessage response, int expectedStatus)
    {
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();

        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        // Status field
        root.TryGetProperty("status", out var statusProp).Should().BeTrue(
            "ProblemDetails must include 'status' field");
        statusProp.GetInt32().Should().Be(expectedStatus);

        // Title field
        root.TryGetProperty("title", out var titleProp).Should().BeTrue(
            "ProblemDetails must include 'title' field");
        titleProp.GetString().Should().NotBeNullOrEmpty();

        // Detail field
        root.TryGetProperty("detail", out var detailProp).Should().BeTrue(
            "ProblemDetails must include 'detail' field");
        detailProp.GetString().Should().NotBeNullOrEmpty();

        // Instance field (RFC 7807 recommends including the request path)
        root.TryGetProperty("instance", out var instanceProp).Should().BeTrue(
            "ProblemDetails must include 'instance' field");
        instanceProp.GetString().Should().NotBeNullOrEmpty();
    }

    private async Task<Guid> SeedSprintWithItemAndDatesAsync(Guid projectId, string name = "Sprint")
    {
        return await WithDbContextAsync(async db =>
        {
            var sprint = SprintBuilder.New()
                .WithProject(projectId)
                .WithName(name)
                .WithStatus(SprintStatus.Planning)
                .WithDates(
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)))
                .Build();
            db.Set<Domain.Entities.Sprint>().Add(sprint);

            var workItem = WorkItemBuilder.New()
                .WithProject(projectId)
                .WithTitle($"Item for {name}")
                .AsTask()
                .WithStatus(WorkItemStatus.ToDo)
                .WithPriority(Priority.Medium)
                .WithSprint(sprint.Id)
                .Build();
            db.Set<Domain.Entities.WorkItem>().Add(workItem);

            await db.SaveChangesAsync();
            return sprint.Id;
        });
    }
}
