using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems.CreateWorkItem;
using TeamFlow.Application.Features.WorkItems.DeleteWorkItem;
using TeamFlow.Application.Features.WorkItems.GetWorkItem;
using TeamFlow.Application.Features.WorkItems.MoveWorkItem;
using TeamFlow.Application.Features.Projects.CreateProject;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Repositories;
using TeamFlow.Tests.Common;
using TeamFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TeamFlow.Api.Tests.WorkItems;

public sealed class WorkItemHierarchyTests : IntegrationTestBase
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
    public async Task HierarchyLifecycle_CreateEpicStoryTask_DeleteEpicCascades()
    {
        var orgId = IntegrationTestBase.SeedOrgId;

        // Create project
        var project = await Sender.Send(new CreateProjectCommand(orgId, "Test Project", null));
        project.IsSuccess.Should().BeTrue();
        var projectId = project.Value.Id;

        // Create Epic
        var epic = await Sender.Send(new CreateWorkItemCommand(projectId, null, WorkItemType.Epic, "Epic 1", null, Priority.High, null));
        epic.IsSuccess.Should().BeTrue();
        var epicId = epic.Value.Id;

        // Create Story under Epic
        var story = await Sender.Send(new CreateWorkItemCommand(projectId, epicId, WorkItemType.UserStory, "Story 1", null, Priority.Medium, null));
        story.IsSuccess.Should().BeTrue();
        var storyId = story.Value.Id;

        // Create Task under Story
        var task = await Sender.Send(new CreateWorkItemCommand(projectId, storyId, WorkItemType.Task, "Task 1", null, Priority.Low, null));
        task.IsSuccess.Should().BeTrue();
        var taskId = task.Value.Id;

        // Verify hierarchy
        var getEpic = await Sender.Send(new GetWorkItemQuery(epicId));
        getEpic.IsSuccess.Should().BeTrue();

        // Delete Epic - should cascade to Story and Task
        var deleteResult = await Sender.Send(new DeleteWorkItemCommand(epicId));
        deleteResult.IsSuccess.Should().BeTrue();

        // Verify items no longer accessible via normal queries (soft-deleted)
        var getDeletedEpic = await Sender.Send(new GetWorkItemQuery(epicId));
        getDeletedEpic.IsFailure.Should().BeTrue();

        var getDeletedStory = await Sender.Send(new GetWorkItemQuery(storyId));
        getDeletedStory.IsFailure.Should().BeTrue();

        var getDeletedTask = await Sender.Send(new GetWorkItemQuery(taskId));
        getDeletedTask.IsFailure.Should().BeTrue();

        // Verify history was recorded for each deleted item
        var historyCount = await DbCtx.WorkItemHistories
            .CountAsync(h => new[] { epicId, storyId, taskId }.Contains(h.WorkItemId) && h.ActionType == "Deleted");
        historyCount.Should().Be(3);
    }

    [Fact]
    public async Task MoveWorkItem_StoryBetweenEpics_Succeeds()
    {
        var orgId = IntegrationTestBase.SeedOrgId;
        var project = await Sender.Send(new CreateProjectCommand(orgId, "Move Test Project", null));
        var projectId = project.Value.Id;

        var epic1 = await Sender.Send(new CreateWorkItemCommand(projectId, null, WorkItemType.Epic, "Epic A", null, Priority.High, null));
        var epic2 = await Sender.Send(new CreateWorkItemCommand(projectId, null, WorkItemType.Epic, "Epic B", null, Priority.High, null));
        var story = await Sender.Send(new CreateWorkItemCommand(projectId, epic1.Value.Id, WorkItemType.UserStory, "Story X", null, Priority.Medium, null));

        story.IsSuccess.Should().BeTrue();
        story.Value.ParentId.Should().Be(epic1.Value.Id);

        // Move story to epic2
        var moveResult = await Sender.Send(new MoveWorkItemCommand(story.Value.Id, epic2.Value.Id));
        moveResult.IsSuccess.Should().BeTrue();

        // Verify new parent
        var getStory = await Sender.Send(new GetWorkItemQuery(story.Value.Id));
        getStory.IsSuccess.Should().BeTrue();
        getStory.Value.ParentId.Should().Be(epic2.Value.Id);
    }
}
