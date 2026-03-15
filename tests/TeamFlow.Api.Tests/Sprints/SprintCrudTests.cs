using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Api.Tests.Infrastructure;
using TeamFlow.Application.Features.Sprints;
using TeamFlow.Application.Features.Sprints.CreateSprint;
using TeamFlow.Application.Features.Sprints.ListSprints;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Tests.Sprints;

[Collection("Integration")]
public sealed class SprintCrudTests(PostgresFixture postgres) : ApiIntegrationTestBase(postgres)
{
    private const string SprintsUrl = "/api/v1/sprints";

    // ── POST /api/v1/sprints ────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidBody_Returns201WithSprintDto()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var response = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Sprint 1", "Deliver MVP", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14))));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var sprint = await response.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);
        sprint.Should().NotBeNull();
        sprint!.Name.Should().Be("Sprint 1");
        sprint.Goal.Should().Be("Deliver MVP");
        sprint.ProjectId.Should().Be(projectId);
        sprint.Status.Should().Be(SprintStatus.Planning);
        sprint.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_WithMissingName_Returns400()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var response = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithEmptyProjectId_Returns400()
    {
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var response = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(Guid.Empty, "Sprint 1", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_AsViewer_Returns403()
    {
        var projectId = await SeedProjectAsync(ProjectRole.Viewer);
        var client = CreateAuthenticatedClient(ProjectRole.Viewer);

        var response = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Sprint 1", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        var client = CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(Guid.NewGuid(), "Sprint 1", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/v1/sprints?projectId=X ─────────────────────────────────────

    [Fact]
    public async Task List_WithValidProject_Returns200WithPaginationShape()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        // Create a sprint first
        await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Sprint 1", null, null, null));

        var response = await client.GetAsync($"{SprintsUrl}?projectId={projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ListSprintsResult>(TestJsonOptions.Default);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task List_EmptyProject_Returns200WithEmptyItems()
    {
        var projectId = await SeedProjectAsync(ProjectRole.Developer);
        var client = CreateAuthenticatedClient(ProjectRole.Developer);

        var response = await client.GetAsync($"{SprintsUrl}?projectId={projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ListSprintsResult>(TestJsonOptions.Default);
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ── GET /api/v1/sprints/{id} ────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingSprint_Returns200WithDetail()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var createResponse = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Detail Sprint", "A goal", null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);

        var response = await client.GetAsync($"{SprintsUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<SprintDetailDto>(TestJsonOptions.Default);
        detail.Should().NotBeNull();
        detail!.Name.Should().Be("Detail Sprint");
        detail.Goal.Should().Be("A goal");
        detail.Items.Should().NotBeNull();
        detail.Capacity.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var projectId = await SeedProjectAsync(ProjectRole.Developer);
        var client = CreateAuthenticatedClient(ProjectRole.Developer);

        var response = await client.GetAsync($"{SprintsUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/v1/sprints/{id} ────────────────────────────────────────────

    [Fact]
    public async Task Update_WithValidBody_Returns200WithUpdatedFields()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var createResponse = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Original Name", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);

        var response = await client.PutAsJsonAsync($"{SprintsUrl}/{created!.Id}",
            new { Name = "Updated Name", Goal = "New Goal", StartDate = (DateOnly?)null, EndDate = (DateOnly?)null });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.Goal.Should().Be("New Goal");
    }

    [Fact]
    public async Task Update_WithEmptyName_Returns400()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var createResponse = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Sprint To Update", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);

        var response = await client.PutAsJsonAsync($"{SprintsUrl}/{created!.Id}",
            new { Name = "", Goal = (string?)null, StartDate = (DateOnly?)null, EndDate = (DateOnly?)null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── DELETE /api/v1/sprints/{id} ─────────────────────────────────────────

    [Fact]
    public async Task Delete_PlanningSprintAsAdmin_Returns204()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var createResponse = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Sprint To Delete", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);

        var response = await client.DeleteAsync($"{SprintsUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ActiveSprint_Returns400()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        // Create sprint with dates and a work item, then start it
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));
        var createResponse = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Active Sprint", null, startDate, endDate));
        var created = await createResponse.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);

        // Seed a work item and add it
        var workItemId = await SeedWorkItemAsync(projectId);
        await client.PostAsync($"{SprintsUrl}/{created!.Id}/items/{workItemId}", null);

        // Start the sprint
        await client.PostAsync($"{SprintsUrl}/{created.Id}/start", null);

        // Try to delete - should fail
        var response = await client.DeleteAsync($"{SprintsUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private async Task<Guid> SeedWorkItemAsync(Guid projectId)
    {
        return await WithDbContextAsync(async db =>
        {
            var workItem = new Domain.Entities.WorkItem
            {
                ProjectId = projectId,
                Title = "Test Work Item",
                Type = WorkItemType.Task,
                Status = WorkItemStatus.ToDo,
                Priority = Priority.Medium
            };
            db.Set<Domain.Entities.WorkItem>().Add(workItem);
            await db.SaveChangesAsync();
            return workItem.Id;
        });
    }
}
