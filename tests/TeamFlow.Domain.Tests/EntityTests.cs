using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Domain.Tests;

public sealed class EntityTests
{
    [Fact]
    public void User_Build_ShouldHaveValidDefaults()
    {
        var user = UserBuilder.New().Build();
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.NotEmpty(user.Email);
        Assert.NotEmpty(user.Name);
        Assert.True(user.CreatedAt > DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void WorkItem_Build_ShouldDefaultToToDo()
    {
        var item = WorkItemBuilder.New().Build();
        Assert.Equal(WorkItemStatus.ToDo, item.Status);
        Assert.Null(item.DeletedAt);
    }

    [Fact]
    public void WorkItem_SoftDelete_ShouldSetDeletedAt()
    {
        var item = WorkItemBuilder.New().Build();
        Assert.Null(item.DeletedAt);

        item.DeletedAt = DateTime.UtcNow;
        Assert.NotNull(item.DeletedAt);
    }

    [Fact]
    public void Sprint_Build_ShouldDefaultToPlanning()
    {
        var sprint = SprintBuilder.New().Build();
        Assert.Equal(SprintStatus.Planning, sprint.Status);
    }

    [Fact]
    public void Organization_Build_ShouldHaveUniqueIds()
    {
        var org1 = OrganizationBuilder.New().Build();
        var org2 = OrganizationBuilder.New().Build();
        Assert.NotEqual(org1.Id, org2.Id);
    }

    [Fact]
    public void Project_Build_ShouldDefaultToActive()
    {
        var project = ProjectBuilder.New().Build();
        Assert.Equal("Active", project.Status);
    }

    [Fact]
    public void WorkItemHistory_ShouldBeReadonly()
    {
        var history = new WorkItemHistory
        {
            WorkItemId = Guid.NewGuid(),
            ActionType = "StatusChanged",
            FieldName = "Status",
            OldValue = "ToDo",
            NewValue = "InProgress"
        };

        Assert.NotEqual(Guid.Empty, history.Id);
        Assert.True(history.CreatedAt > DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void WorkItem_WithAllFields_ShouldBuildCorrectly()
    {
        var projectId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var item = WorkItemBuilder.New()
            .WithProject(projectId)
            .WithType(WorkItemType.UserStory)
            .WithTitle("As a user, I can log in")
            .WithPriority(Priority.High)
            .WithAssignee(assigneeId)
            .WithEstimation(5)
            .Build();

        Assert.Equal(projectId, item.ProjectId);
        Assert.Equal(WorkItemType.UserStory, item.Type);
        Assert.Equal("As a user, I can log in", item.Title);
        Assert.Equal(Priority.High, item.Priority);
        Assert.Equal(assigneeId, item.AssigneeId);
        Assert.Equal(5m, item.EstimationValue);
    }
}
