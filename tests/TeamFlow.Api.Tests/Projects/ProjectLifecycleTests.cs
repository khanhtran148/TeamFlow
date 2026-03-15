using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects.ArchiveProject;
using TeamFlow.Application.Features.Projects.CreateProject;
using TeamFlow.Application.Features.Projects.DeleteProject;
using TeamFlow.Application.Features.Projects.GetProject;
using TeamFlow.Application.Features.Projects.ListProjects;
using TeamFlow.Application.Features.Projects.UpdateProject;
using TeamFlow.Infrastructure.Repositories;
using TeamFlow.Tests.Common;

namespace TeamFlow.Api.Tests.Projects;

public sealed class ProjectLifecycleTests : IntegrationTestBase
{
    private ISender Sender => Services.GetRequiredService<ISender>();
    private static readonly Guid OrgId = IntegrationTestBase.SeedOrgId;

    protected override Task ConfigureServices(IServiceCollection services)
    {
        services.AddApplication();

        // Repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IReleaseRepository, ReleaseRepository>();
        services.AddScoped<IWorkItemRepository, WorkItemRepository>();
        services.AddScoped<IWorkItemLinkRepository, WorkItemLinkRepository>();

        // Stubs
        services.AddScoped<ICurrentUser, TestCurrentUser>();
        services.AddScoped<IPermissionChecker, AlwaysAllowTestPermissionChecker>();
        services.AddScoped<IHistoryService, TestHistoryService>();
        services.AddScoped<IBroadcastService, NullBroadcastService>();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ProjectLifecycle_CreateUpdateArchiveListDelete()
    {
        // Step 1: Create
        var createCmd = new CreateProjectCommand(OrgId, "Alpha Project", "First project");
        var createResult = await Sender.Send(createCmd);
        createResult.IsSuccess.Should().BeTrue();
        var projectId = createResult.Value.Id;

        // Step 2: Update
        var updateResult = await Sender.Send(new UpdateProjectCommand(projectId, "Alpha Project Updated", "Updated description"));
        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Value.Name.Should().Be("Alpha Project Updated");

        // Step 3: Get by ID
        var getResult = await Sender.Send(new GetProjectQuery(projectId));
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Name.Should().Be("Alpha Project Updated");

        // Step 4: Archive
        var archiveResult = await Sender.Send(new ArchiveProjectCommand(projectId));
        archiveResult.IsSuccess.Should().BeTrue();

        // Step 5: Verify not in active list
        var activeList = await Sender.Send(new ListProjectsQuery(OrgId, "Active", null, 1, 20));
        activeList.IsSuccess.Should().BeTrue();
        activeList.Value.Items.Should().NotContain(p => p.Id == projectId);

        // Step 6: Verify in archived list
        var archivedList = await Sender.Send(new ListProjectsQuery(OrgId, "Archived", null, 1, 20));
        archivedList.IsSuccess.Should().BeTrue();
        archivedList.Value.Items.Should().Contain(p => p.Id == projectId);

        // Step 7: Delete
        var deleteResult = await Sender.Send(new DeleteProjectCommand(projectId));
        deleteResult.IsSuccess.Should().BeTrue();
    }
}

internal sealed class TestHistoryService : IHistoryService
{
    public Task RecordAsync(WorkItemHistoryEntry entry, CancellationToken ct = default) => Task.CompletedTask;
}
