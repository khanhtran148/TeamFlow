using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Api.Tests.Infrastructure;
using TeamFlow.Application.Features.Retros;
using TeamFlow.Application.Features.Retros.CreateRetroSession;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Tests.Retros;

[Collection("Integration")]
public sealed class RetroCrudTests(PostgresFixture postgres) : ApiIntegrationTestBase(postgres)
{
    private const string RetrosUrl = "/api/v1/retros";

    // ── POST /api/v1/retros ──────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidBody_Returns201WithSessionDto()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var response = await client.PostAsJsonAsync(RetrosUrl,
            new CreateRetroSessionCommand(projectId, null, null, "Public"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var session = await response.Content.ReadFromJsonAsync<RetroSessionDto>(TestJsonOptions.Default);
        session.Should().NotBeNull();
        session!.ProjectId.Should().Be(projectId);
        session.AnonymityMode.Should().Be("Public");
        session.Status.Should().Be(RetroSessionStatus.Draft);
        session.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_AsViewer_Returns403()
    {
        var projectId = await SeedProjectAsync(ProjectRole.Viewer);
        var client = CreateAuthenticatedClient(ProjectRole.Viewer);

        var response = await client.PostAsJsonAsync(RetrosUrl,
            new CreateRetroSessionCommand(projectId, null, null, "Public"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/v1/retros/{id} ──────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingSession_Returns200()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var createResponse = await client.PostAsJsonAsync(RetrosUrl,
            new CreateRetroSessionCommand(projectId, null, null, "Public"));
        var created = await createResponse.Content.ReadFromJsonAsync<RetroSessionDto>(TestJsonOptions.Default);

        var response = await client.GetAsync($"{RetrosUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var session = await response.Content.ReadFromJsonAsync<RetroSessionDto>(TestJsonOptions.Default);
        session.Should().NotBeNull();
        session!.Id.Should().Be(created.Id);
        session.ProjectId.Should().Be(projectId);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var response = await client.GetAsync($"{RetrosUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/v1/retros/{id}/start ───────────────────────────────────

    [Fact]
    public async Task Start_DraftSession_Returns200()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var createResponse = await client.PostAsJsonAsync(RetrosUrl,
            new CreateRetroSessionCommand(projectId, null, null, "Public"));
        var created = await createResponse.Content.ReadFromJsonAsync<RetroSessionDto>(TestJsonOptions.Default);

        var response = await client.PostAsync($"{RetrosUrl}/{created!.Id}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var session = await response.Content.ReadFromJsonAsync<RetroSessionDto>(TestJsonOptions.Default);
        session.Should().NotBeNull();
        session!.Status.Should().Be(RetroSessionStatus.Open);
    }

    // ── POST /api/v1/retros/{id}/cards ───────────────────────────────────

    [Fact]
    public async Task SubmitCard_OpenSession_Returns201()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var createResponse = await client.PostAsJsonAsync(RetrosUrl,
            new CreateRetroSessionCommand(projectId, null, null, "Public"));
        var created = await createResponse.Content.ReadFromJsonAsync<RetroSessionDto>(TestJsonOptions.Default);

        await client.PostAsync($"{RetrosUrl}/{created!.Id}/start", null);

        var response = await client.PostAsJsonAsync($"{RetrosUrl}/{created.Id}/cards",
            new { Category = RetroCardCategory.WentWell, Content = "Great teamwork!" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var card = await response.Content.ReadFromJsonAsync<RetroCardDto>(TestJsonOptions.Default);
        card.Should().NotBeNull();
        card!.Content.Should().Be("Great teamwork!");
        card.Category.Should().Be(RetroCardCategory.WentWell);
    }

    [Fact]
    public async Task SubmitCard_AsViewer_Returns403()
    {
        // Seed a project where the test user is a Viewer (not OrgAdmin).
        // The Viewer role does not include Retro_SubmitCard permission.
        var projectId = await SeedProjectAsync(ProjectRole.Viewer);
        var client = CreateAuthenticatedClient(ProjectRole.Viewer);

        // Seed an open retro session directly in the DB since Viewer cannot create one.
        var sessionId = await SeedOpenRetroSessionAsync(projectId);

        var response = await client.PostAsJsonAsync($"{RetrosUrl}/{sessionId}/cards",
            new { Category = RetroCardCategory.WentWell, Content = "Viewer attempt" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/v1/retros/{id}/close ───────────────────────────────────

    [Fact]
    public async Task Close_DiscussingSession_Returns200()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        var createResponse = await client.PostAsJsonAsync(RetrosUrl,
            new CreateRetroSessionCommand(projectId, null, null, "Public"));
        var created = await createResponse.Content.ReadFromJsonAsync<RetroSessionDto>(TestJsonOptions.Default);

        // Transition: Draft -> Open -> Voting -> Discussing
        await client.PostAsync($"{RetrosUrl}/{created!.Id}/start", null);
        await client.PostAsJsonAsync($"{RetrosUrl}/{created.Id}/transition",
            new { TargetStatus = RetroSessionStatus.Voting });
        await client.PostAsJsonAsync($"{RetrosUrl}/{created.Id}/transition",
            new { TargetStatus = RetroSessionStatus.Discussing });

        var response = await client.PostAsync($"{RetrosUrl}/{created.Id}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var session = await response.Content.ReadFromJsonAsync<RetroSessionDto>(TestJsonOptions.Default);
        session.Should().NotBeNull();
        session!.Status.Should().Be(RetroSessionStatus.Closed);
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        var client = CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(RetrosUrl,
            new CreateRetroSessionCommand(Guid.NewGuid(), null, null, "Public"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private async Task<Guid> SeedOpenRetroSessionAsync(Guid projectId)
    {
        return await WithDbContextAsync(async db =>
        {
            var session = new Domain.Entities.RetroSession
            {
                ProjectId = projectId,
                FacilitatorId = SeedUser2Id,
                AnonymityMode = "Public",
                Status = RetroSessionStatus.Open
            };
            db.Set<Domain.Entities.RetroSession>().Add(session);
            await db.SaveChangesAsync();
            return session.Id;
        });
    }
}
