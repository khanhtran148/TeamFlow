using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Api.Tests.Infrastructure;
using TeamFlow.Application.Features.Sprints;
using TeamFlow.Application.Features.Sprints.CreateSprint;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Tests.Sprints;

/// <summary>
/// Verifies the permission matrix for Sprint endpoints across all 6 project roles.
/// Uses [Theory] with [MemberData] to generate role x endpoint combinations.
/// </summary>
[Collection("Integration")]
public sealed class SprintPermissionMatrixTests(PostgresFixture postgres) : ApiIntegrationTestBase(postgres)
{
    private const string SprintsUrl = "/api/v1/sprints";

    // ── Read endpoints: all roles get 200 ───────────────────────────────────

    [Theory]
    [MemberData(nameof(AllRoles))]
    public async Task ListSprints_AllRoles_Returns200(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var client = CreateAuthenticatedClient(role);

        var response = await client.GetAsync($"{SprintsUrl}?projectId={projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(AllRoles))]
    public async Task GetSprint_AllRoles_Returns200(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedSprintAsync(projectId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.GetAsync($"{SprintsUrl}/{sprintId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(AllRoles))]
    public async Task GetBurndown_AllRoles_Returns200(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedSprintAsync(projectId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.GetAsync($"{SprintsUrl}/{sprintId}/burndown");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Create sprint: Admin/PO/TL/TM get 201, Dev/Viewer get 403 ──────────

    [Theory]
    [MemberData(nameof(RolesWithSprintCreate))]
    public async Task CreateSprint_AllowedRoles_Returns201(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var client = CreateAuthenticatedClient(role);

        var response = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "New Sprint", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Theory]
    [MemberData(nameof(RolesWithoutSprintCreate))]
    public async Task CreateSprint_DeniedRoles_Returns403(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var client = CreateAuthenticatedClient(role);

        var response = await client.PostAsJsonAsync(SprintsUrl,
            new CreateSprintCommand(projectId, "New Sprint", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Update sprint: Admin/PO/TL/TM get 200, Dev/Viewer get 403 ──────────

    [Theory]
    [MemberData(nameof(RolesWithSprintEdit))]
    public async Task UpdateSprint_AllowedRoles_Returns200(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedSprintAsync(projectId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.PutAsJsonAsync($"{SprintsUrl}/{sprintId}",
            new { Name = "Updated Sprint", Goal = (string?)null, StartDate = (DateOnly?)null, EndDate = (DateOnly?)null });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(RolesWithoutSprintEdit))]
    public async Task UpdateSprint_DeniedRoles_Returns403(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedSprintAsync(projectId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.PutAsJsonAsync($"{SprintsUrl}/{sprintId}",
            new { Name = "Updated Sprint", Goal = (string?)null, StartDate = (DateOnly?)null, EndDate = (DateOnly?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Delete sprint: Admin/PO/TL/TM get 204, Dev/Viewer get 403 ──────────

    [Theory]
    [MemberData(nameof(RolesWithSprintEdit))]
    public async Task DeleteSprint_AllowedRoles_Returns204(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedSprintAsync(projectId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.DeleteAsync($"{SprintsUrl}/{sprintId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [MemberData(nameof(RolesWithoutSprintEdit))]
    public async Task DeleteSprint_DeniedRoles_Returns403(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedSprintAsync(projectId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.DeleteAsync($"{SprintsUrl}/{sprintId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Start sprint: OrgAdmin/TeamManager get 200, others get 403 ──────────

    [Theory]
    [MemberData(nameof(RolesWithSprintStart))]
    public async Task StartSprint_AllowedRoles_Returns200(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedSprintWithItemAndDatesAsync(projectId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.PostAsync($"{SprintsUrl}/{sprintId}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(RolesWithoutSprintStart))]
    public async Task StartSprint_DeniedRoles_Returns403(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedSprintWithItemAndDatesAsync(projectId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.PostAsync($"{SprintsUrl}/{sprintId}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Complete sprint: OrgAdmin/TeamManager get 200, others get 403 ───────

    [Theory]
    [MemberData(nameof(RolesWithSprintComplete))]
    public async Task CompleteSprint_AllowedRoles_Returns200(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedActiveSprintAsync(projectId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.PostAsync($"{SprintsUrl}/{sprintId}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(RolesWithoutSprintComplete))]
    public async Task CompleteSprint_DeniedRoles_Returns403(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedActiveSprintAsync(projectId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.PostAsync($"{SprintsUrl}/{sprintId}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── AddItem: Admin/PO/TL/TM get 200, Dev/Viewer get 403 ────────────────

    [Theory]
    [MemberData(nameof(RolesWithSprintEdit))]
    public async Task AddItem_AllowedRoles_Returns200(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedSprintAsync(projectId);
        var workItemId = await SeedWorkItemAsync(projectId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.PostAsync($"{SprintsUrl}/{sprintId}/items/{workItemId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(RolesWithoutSprintEdit))]
    public async Task AddItem_DeniedRoles_Returns403(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedSprintAsync(projectId);
        var workItemId = await SeedWorkItemAsync(projectId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.PostAsync($"{SprintsUrl}/{sprintId}/items/{workItemId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── RemoveItem: Admin/PO/TL/TM get 204, Dev/Viewer get 403 ─────────────

    [Theory]
    [MemberData(nameof(RolesWithSprintEdit))]
    public async Task RemoveItem_AllowedRoles_Returns204(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedSprintAsync(projectId);
        var workItemId = await SeedWorkItemAsync(projectId, sprintId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.DeleteAsync($"{SprintsUrl}/{sprintId}/items/{workItemId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [MemberData(nameof(RolesWithoutSprintEdit))]
    public async Task RemoveItem_DeniedRoles_Returns403(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedSprintAsync(projectId);
        var workItemId = await SeedWorkItemAsync(projectId, sprintId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.DeleteAsync($"{SprintsUrl}/{sprintId}/items/{workItemId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── UpdateCapacity: Admin/PO/TL/TM get 200, Dev/Viewer get 403 ─────────

    [Theory]
    [MemberData(nameof(RolesWithSprintEdit))]
    public async Task UpdateCapacity_AllowedRoles_Returns200(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedSprintAsync(projectId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.PutAsJsonAsync($"{SprintsUrl}/{sprintId}/capacity",
            new { Capacity = new[] { new { MemberId = SeedUserId, Points = 13 } } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(RolesWithoutSprintEdit))]
    public async Task UpdateCapacity_DeniedRoles_Returns403(ProjectRole role)
    {
        var projectId = await SeedProjectAsync(role);
        var sprintId = await SeedSprintAsync(projectId);
        var client = CreateAuthenticatedClient(role);

        var response = await client.PutAsJsonAsync($"{SprintsUrl}/{sprintId}/capacity",
            new { Capacity = new[] { new { MemberId = SeedUserId, Points = 13 } } });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── MemberData sources ──────────────────────────────────────────────────

    public static TheoryData<ProjectRole> AllRoles => new()
    {
        ProjectRole.OrgAdmin,
        ProjectRole.ProductOwner,
        ProjectRole.TechnicalLeader,
        ProjectRole.TeamManager,
        ProjectRole.Developer,
        ProjectRole.Viewer
    };

    /// <summary>Roles with Sprint_Create permission.</summary>
    public static TheoryData<ProjectRole> RolesWithSprintCreate => new()
    {
        ProjectRole.OrgAdmin,
        ProjectRole.ProductOwner,
        ProjectRole.TechnicalLeader,
        ProjectRole.TeamManager
    };

    /// <summary>Roles without Sprint_Create permission.</summary>
    public static TheoryData<ProjectRole> RolesWithoutSprintCreate => new()
    {
        ProjectRole.Developer,
        ProjectRole.Viewer
    };

    /// <summary>Roles with Sprint_Edit permission.</summary>
    public static TheoryData<ProjectRole> RolesWithSprintEdit => new()
    {
        ProjectRole.OrgAdmin,
        ProjectRole.ProductOwner,
        ProjectRole.TechnicalLeader,
        ProjectRole.TeamManager
    };

    /// <summary>Roles without Sprint_Edit permission.</summary>
    public static TheoryData<ProjectRole> RolesWithoutSprintEdit => new()
    {
        ProjectRole.Developer,
        ProjectRole.Viewer
    };

    /// <summary>Roles with Sprint_Start permission (OrgAdmin has all; TeamManager has explicit).</summary>
    public static TheoryData<ProjectRole> RolesWithSprintStart => new()
    {
        ProjectRole.OrgAdmin,
        ProjectRole.TeamManager
    };

    /// <summary>Roles without Sprint_Start permission.</summary>
    public static TheoryData<ProjectRole> RolesWithoutSprintStart => new()
    {
        ProjectRole.ProductOwner,
        ProjectRole.TechnicalLeader,
        ProjectRole.Developer,
        ProjectRole.Viewer
    };

    /// <summary>Roles with Sprint_Complete permission.</summary>
    public static TheoryData<ProjectRole> RolesWithSprintComplete => new()
    {
        ProjectRole.OrgAdmin,
        ProjectRole.TeamManager
    };

    /// <summary>Roles without Sprint_Complete permission.</summary>
    public static TheoryData<ProjectRole> RolesWithoutSprintComplete => new()
    {
        ProjectRole.ProductOwner,
        ProjectRole.TechnicalLeader,
        ProjectRole.Developer,
        ProjectRole.Viewer
    };

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Guid> SeedSprintAsync(Guid projectId)
    {
        return await WithDbContextAsync(async db =>
        {
            var sprint = new Domain.Entities.Sprint
            {
                ProjectId = projectId,
                Name = "Test Sprint",
                Status = SprintStatus.Planning
            };
            db.Set<Domain.Entities.Sprint>().Add(sprint);
            await db.SaveChangesAsync();
            return sprint.Id;
        });
    }

    private async Task<Guid> SeedSprintWithItemAndDatesAsync(Guid projectId)
    {
        return await WithDbContextAsync(async db =>
        {
            var sprint = new Domain.Entities.Sprint
            {
                ProjectId = projectId,
                Name = "Sprint To Start",
                Status = SprintStatus.Planning,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14))
            };
            db.Set<Domain.Entities.Sprint>().Add(sprint);

            var workItem = new Domain.Entities.WorkItem
            {
                ProjectId = projectId,
                Title = "Seed Item",
                Type = WorkItemType.Task,
                Status = WorkItemStatus.ToDo,
                Priority = Priority.Medium,
                SprintId = sprint.Id
            };
            db.Set<Domain.Entities.WorkItem>().Add(workItem);

            await db.SaveChangesAsync();
            return sprint.Id;
        });
    }

    private async Task<Guid> SeedActiveSprintAsync(Guid projectId)
    {
        return await WithDbContextAsync(async db =>
        {
            var sprint = new Domain.Entities.Sprint
            {
                ProjectId = projectId,
                Name = "Active Sprint",
                Status = SprintStatus.Active,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
            };
            db.Set<Domain.Entities.Sprint>().Add(sprint);

            var workItem = new Domain.Entities.WorkItem
            {
                ProjectId = projectId,
                Title = "Active Item",
                Type = WorkItemType.Task,
                Status = WorkItemStatus.InProgress,
                Priority = Priority.Medium,
                SprintId = sprint.Id
            };
            db.Set<Domain.Entities.WorkItem>().Add(workItem);

            await db.SaveChangesAsync();
            return sprint.Id;
        });
    }

    private async Task<Guid> SeedWorkItemAsync(Guid projectId, Guid? sprintId = null)
    {
        return await WithDbContextAsync(async db =>
        {
            var workItem = new Domain.Entities.WorkItem
            {
                ProjectId = projectId,
                Title = "Test Work Item",
                Type = WorkItemType.Task,
                Status = WorkItemStatus.ToDo,
                Priority = Priority.Medium,
                SprintId = sprintId
            };
            db.Set<Domain.Entities.WorkItem>().Add(workItem);
            await db.SaveChangesAsync();
            return workItem.Id;
        });
    }
}
