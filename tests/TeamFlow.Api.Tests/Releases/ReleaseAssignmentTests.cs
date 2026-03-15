using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects.CreateProject;
using TeamFlow.Application.Features.Releases.AssignItem;
using TeamFlow.Application.Features.Releases.CreateRelease;
using TeamFlow.Application.Features.Releases.DeleteRelease;
using TeamFlow.Application.Features.Releases.GetRelease;
using TeamFlow.Application.Features.WorkItems.CreateWorkItem;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Infrastructure.Repositories;
using TeamFlow.Tests.Common;

namespace TeamFlow.Api.Tests.Releases;

public sealed class ReleaseAssignmentTests : IntegrationTestBase
{
    private ISender Sender => Services.GetRequiredService<ISender>();
    private TeamFlowDbContext DbCtx => Services.GetRequiredService<TeamFlowDbContext>();

    protected override Task ConfigureServices(IServiceCollection services)
    {
        services.AddApplication();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IReleaseRepository, ReleaseRepository>();
        services.AddScoped<IWorkItemRepository, WorkItemRepository>();
        services.AddScoped<IWorkItemLinkRepository, WorkItemLinkRepository>();
        services.AddScoped<ICurrentUser, TestCurrentUser>();
        services.AddScoped<IPermissionChecker, AlwaysAllowTestPermissionChecker>();
        services.AddScoped<IHistoryService, Infrastructure.Services.HistoryService>();
        services.AddScoped<IBroadcastService, NullBroadcastService>();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ReleaseLifecycle_CreateAssignVerifyDeleteUnlinks()
    {
        // Create project
        var project = await Sender.Send(new CreateProjectCommand(IntegrationTestBase.SeedOrgId, "Release Test Project", null));
        var projectId = project.Value.Id;

        // Create release
        var release = await Sender.Send(new CreateReleaseCommand(projectId, "v1.0.0", "First release", null));
        release.IsSuccess.Should().BeTrue();
        var releaseId = release.Value.Id;

        // Create work item
        var item = await Sender.Send(new CreateWorkItemCommand(projectId, null, WorkItemType.UserStory, "Story 1", null, Priority.Medium, null));
        var itemId = item.Value.Id;

        // Assign item to release
        var assignResult = await Sender.Send(new AssignItemToReleaseCommand(releaseId, itemId));
        assignResult.IsSuccess.Should().BeTrue();

        // Verify item is in release
        var workItem = await DbCtx.WorkItems.FindAsync(itemId);
        workItem!.ReleaseId.Should().Be(releaseId);

        // Verify one-release constraint: create another release and try to assign the same item
        var release2 = await Sender.Send(new CreateReleaseCommand(projectId, "v2.0.0", null, null));
        var assignToOther = await Sender.Send(new AssignItemToReleaseCommand(release2.Value.Id, itemId));
        assignToOther.IsFailure.Should().BeTrue();
        assignToOther.Error.Should().Contain("another release");

        // Get release with item counts
        var getReleaseResult = await Sender.Send(new GetReleaseQuery(releaseId));
        getReleaseResult.IsSuccess.Should().BeTrue();
        getReleaseResult.Value.TotalItems.Should().Be(1);

        // Delete release - items should be unlinked
        var deleteResult = await Sender.Send(new DeleteReleaseCommand(releaseId));
        deleteResult.IsSuccess.Should().BeTrue();

        // Verify item is unlinked
        var workItemAfter = await DbCtx.WorkItems.FindAsync(itemId);
        workItemAfter!.ReleaseId.Should().BeNull();
    }
}
