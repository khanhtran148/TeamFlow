using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Api.Tests.Infrastructure;
using TeamFlow.Application.Features.Sprints;
using TeamFlow.Application.Features.Sprints.CreateSprint;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Api.Tests.Sprints;

[Collection("Integration")]
public sealed class SprintLifecycleTests(PostgresFixture postgres) : ApiIntegrationTestBase(postgres)
{
    private const string SprintsUrl = "/api/v1/sprints";

    // ── POST /sprints/{id}/start ────────────────────────────────────────────

    [Fact]
    public async Task Start_PlanningSprintWithItemsAndDates_Returns200()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var sprintId = await CreateSprintWithItemAsync(client, projectId);

        var response = await client.PostAsync($"{SprintsUrl}/{sprintId}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var sprint = await response.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);
        sprint.Should().NotBeNull();
        sprint!.Status.Should().Be(SprintStatus.Active);
    }

    [Fact]
    public async Task Start_SprintWithNoItems_Returns400()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));
        var createResponse = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Empty Sprint", null, startDate, endDate));
        var created = await createResponse.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);

        var response = await client.PostAsync($"{SprintsUrl}/{created!.Id}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Start_WhenAnotherSprintAlreadyActive_Returns409()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        // Start the first sprint
        var sprint1Id = await CreateSprintWithItemAsync(client, projectId);
        await client.PostAsync($"{SprintsUrl}/{sprint1Id}/start", null);

        // Try to start a second sprint
        var sprint2Id = await CreateSprintWithItemAsync(client, projectId, "Sprint 2");

        var response = await client.PostAsync($"{SprintsUrl}/{sprint2Id}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── POST /sprints/{id}/complete ─────────────────────────────────────────

    [Fact]
    public async Task Complete_ActiveSprint_Returns200()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var sprintId = await CreateSprintWithItemAsync(client, projectId);
        await client.PostAsync($"{SprintsUrl}/{sprintId}/start", null);

        var response = await client.PostAsync($"{SprintsUrl}/{sprintId}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var sprint = await response.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);
        sprint.Should().NotBeNull();
        sprint!.Status.Should().Be(SprintStatus.Completed);
    }

    [Fact]
    public async Task Complete_PlanningSprintNotActive_Returns400()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var createResponse = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Planning Sprint", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);

        var response = await client.PostAsync($"{SprintsUrl}/{created!.Id}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /sprints/{id}/items/{workItemId} ───────────────────────────────

    [Fact]
    public async Task AddItem_ToPlanningSprint_Returns200()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var createResponse = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Sprint Add", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);

        var workItemId = await SeedWorkItemAsync(projectId);

        var response = await client.PostAsync($"{SprintsUrl}/{created!.Id}/items/{workItemId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddItem_AlreadyInAnotherSprint_Returns409()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        // Create two sprints
        var sprint1Response = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Sprint A", null, null, null));
        var sprint1 = await sprint1Response.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);

        var sprint2Response = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Sprint B", null, null, null));
        var sprint2 = await sprint2Response.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);

        var workItemId = await SeedWorkItemAsync(projectId);

        // Add to sprint 1
        await client.PostAsync($"{SprintsUrl}/{sprint1!.Id}/items/{workItemId}", null);

        // Try to add same item to sprint 2
        var response = await client.PostAsync($"{SprintsUrl}/{sprint2!.Id}/items/{workItemId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── DELETE /sprints/{id}/items/{workItemId} ─────────────────────────────

    [Fact]
    public async Task RemoveItem_ExistingItem_Returns204()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var createResponse = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Sprint Remove", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);

        var workItemId = await SeedWorkItemAsync(projectId);
        await client.PostAsync($"{SprintsUrl}/{created!.Id}/items/{workItemId}", null);

        var response = await client.DeleteAsync($"{SprintsUrl}/{created.Id}/items/{workItemId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── PUT /sprints/{id}/capacity ──────────────────────────────────────────

    [Fact]
    public async Task UpdateCapacity_PlanningSprintValidEntries_Returns200()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var createResponse = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "Sprint Capacity", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);

        var response = await client.PutAsJsonAsync($"{SprintsUrl}/{created!.Id}/capacity",
            new { Capacity = new[] { new { MemberId = SeedUserId, Points = 21 } } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateCapacity_ActiveSprint_Returns400()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var sprintId = await CreateSprintWithItemAsync(client, projectId);
        await client.PostAsync($"{SprintsUrl}/{sprintId}/start", null);

        var response = await client.PutAsJsonAsync($"{SprintsUrl}/{sprintId}/capacity",
            new { Capacity = new[] { new { MemberId = SeedUserId, Points = 10 } } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /sprints/{id}/burndown ──────────────────────────────────────────

    [Fact]
    public async Task GetBurndown_ExistingSprint_Returns200WithDataShape()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var sprintId = await CreateSprintWithItemAsync(client, projectId);

        var response = await client.GetAsync($"{SprintsUrl}/{sprintId}/burndown");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var burndown = await response.Content.ReadFromJsonAsync<BurndownDto>(TestJsonOptions.Default);
        burndown.Should().NotBeNull();
        burndown!.SprintId.Should().Be(sprintId);
        burndown.IdealLine.Should().NotBeNull();
        burndown.ActualLine.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBurndown_NonExistentSprint_Returns404()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var response = await client.GetAsync($"{SprintsUrl}/{Guid.NewGuid()}/burndown");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Guid> SeedWorkItemAsync(Guid projectId)
    {
        return await WithDbContextAsync(async db =>
        {
            var workItem = WorkItemBuilder.New()
                .WithProject(projectId)
                .WithTitle("Test Work Item")
                .AsTask()
                .WithStatus(WorkItemStatus.ToDo)
                .WithPriority(Priority.Medium)
                .Build();
            db.Set<Domain.Entities.WorkItem>().Add(workItem);
            await db.SaveChangesAsync();
            return workItem.Id;
        });
    }

    private async Task<Guid> CreateSprintWithItemAsync(
        HttpClient client, Guid projectId, string name = "Sprint With Item")
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));
        var createResponse = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, name, null, startDate, endDate));
        var created = await createResponse.Content.ReadFromJsonAsync<SprintDto>(TestJsonOptions.Default);

        var workItemId = await SeedWorkItemAsync(projectId);
        await client.PostAsync($"{SprintsUrl}/{created!.Id}/items/{workItemId}", null);

        return created.Id;
    }
}
