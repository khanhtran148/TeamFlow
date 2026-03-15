using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Domain;

/// <summary>
/// Tests for domain enums — verify all values defined per the data model.
/// TFD: These tests verify enum values match the schema spec.
/// </summary>
public class EnumTests
{
    [Fact]
    public void ProjectRole_ShouldHaveSixValues()
    {
        var values = Enum.GetValues<ProjectRole>();
        Assert.Equal(6, values.Length);
        Assert.Contains(ProjectRole.OrgAdmin, values);
        Assert.Contains(ProjectRole.ProductOwner, values);
        Assert.Contains(ProjectRole.TechnicalLeader, values);
        Assert.Contains(ProjectRole.TeamManager, values);
        Assert.Contains(ProjectRole.Developer, values);
        Assert.Contains(ProjectRole.Viewer, values);
    }

    [Fact]
    public void WorkItemType_ShouldHaveFiveValues()
    {
        var values = Enum.GetValues<WorkItemType>();
        Assert.Equal(5, values.Length);
        Assert.Contains(WorkItemType.Epic, values);
        Assert.Contains(WorkItemType.UserStory, values);
        Assert.Contains(WorkItemType.Task, values);
        Assert.Contains(WorkItemType.Bug, values);
        Assert.Contains(WorkItemType.Spike, values);
    }

    [Fact]
    public void WorkItemStatus_ShouldHaveSixValues()
    {
        var values = Enum.GetValues<WorkItemStatus>();
        Assert.Equal(6, values.Length);
        Assert.Contains(WorkItemStatus.ToDo, values);
        Assert.Contains(WorkItemStatus.InProgress, values);
        Assert.Contains(WorkItemStatus.InReview, values);
        Assert.Contains(WorkItemStatus.NeedsClarification, values);
        Assert.Contains(WorkItemStatus.Done, values);
        Assert.Contains(WorkItemStatus.Rejected, values);
    }

    [Fact]
    public void Priority_ShouldHaveFourValues()
    {
        var values = Enum.GetValues<Priority>();
        Assert.Equal(4, values.Length);
    }

    [Fact]
    public void LinkType_ShouldHaveSixValues()
    {
        var values = Enum.GetValues<LinkType>();
        Assert.Equal(6, values.Length);
        Assert.Contains(LinkType.Blocks, values);
        Assert.Contains(LinkType.RelatesTo, values);
        Assert.Contains(LinkType.Duplicates, values);
        Assert.Contains(LinkType.DependsOn, values);
        Assert.Contains(LinkType.Causes, values);
        Assert.Contains(LinkType.Clones, values);
    }

    [Fact]
    public void ReleaseStatus_ShouldHaveThreeValues()
    {
        var values = Enum.GetValues<ReleaseStatus>();
        Assert.Equal(3, values.Length);
        Assert.Contains(ReleaseStatus.Unreleased, values);
        Assert.Contains(ReleaseStatus.Overdue, values);
        Assert.Contains(ReleaseStatus.Released, values);
    }

    [Fact]
    public void SprintStatus_ShouldHaveThreeValues()
    {
        var values = Enum.GetValues<SprintStatus>();
        Assert.Equal(3, values.Length);
        Assert.Contains(SprintStatus.Planning, values);
        Assert.Contains(SprintStatus.Active, values);
        Assert.Contains(SprintStatus.Completed, values);
    }

    [Fact]
    public void RetroSessionStatus_ShouldHaveFiveValues()
    {
        var values = Enum.GetValues<RetroSessionStatus>();
        Assert.Equal(5, values.Length);
    }

    [Fact]
    public void RetroCardCategory_ShouldHaveThreeValues()
    {
        var values = Enum.GetValues<RetroCardCategory>();
        Assert.Equal(3, values.Length);
        Assert.Contains(RetroCardCategory.WentWell, values);
        Assert.Contains(RetroCardCategory.NeedsImprovement, values);
        Assert.Contains(RetroCardCategory.ActionItem, values);
    }
}
