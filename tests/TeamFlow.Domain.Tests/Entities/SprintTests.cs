using FluentAssertions;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Domain.Tests.Entities;

public sealed class SprintTests
{
    [Fact]
    public void Sprint_IsSealed()
    {
        typeof(Sprint).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void Start_PlanningSprintWithItemsAndDates_TransitionsToActive()
    {
        var sprint = SprintBuilder.New()
            .WithDates(new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 30))
            .Build();
        sprint.WorkItems.Add(WorkItemBuilder.New().Build());

        var result = sprint.Start();

        result.IsSuccess.Should().BeTrue();
        sprint.Status.Should().Be(SprintStatus.Active);
    }

    [Fact]
    public void Start_ActiveSprint_ReturnsFailure()
    {
        var sprint = SprintBuilder.New().Active().Build();

        var result = sprint.Start();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Planning");
    }

    [Fact]
    public void Start_CompletedSprint_ReturnsFailure()
    {
        var sprint = SprintBuilder.New().Completed().Build();

        var result = sprint.Start();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Planning");
    }

    [Fact]
    public void Start_SprintWithoutDates_ReturnsFailure()
    {
        var sprint = SprintBuilder.New().Build();
        sprint.WorkItems.Add(WorkItemBuilder.New().Build());

        var result = sprint.Start();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("dates");
    }

    [Fact]
    public void Start_SprintWithoutItems_ReturnsFailure()
    {
        var sprint = SprintBuilder.New()
            .WithDates(new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 30))
            .Build();

        var result = sprint.Start();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("item");
    }

    [Fact]
    public void Complete_ActiveSprint_TransitionsToCompleted()
    {
        var sprint = SprintBuilder.New().Active().Build();
        sprint.WorkItems.Add(WorkItemBuilder.New().WithStatus(WorkItemStatus.Done).Build());

        var result = sprint.Complete();

        result.IsSuccess.Should().BeTrue();
        sprint.Status.Should().Be(SprintStatus.Completed);
    }

    [Fact]
    public void Complete_ActiveSprint_ReturnsIncompleteItemIds()
    {
        var sprint = SprintBuilder.New().Active().Build();
        var doneItem = WorkItemBuilder.New().WithStatus(WorkItemStatus.Done).Build();
        var todoItem = WorkItemBuilder.New().WithStatus(WorkItemStatus.ToDo).Build();
        var rejectedItem = WorkItemBuilder.New().WithStatus(WorkItemStatus.Rejected).Build();
        sprint.WorkItems.Add(doneItem);
        sprint.WorkItems.Add(todoItem);
        sprint.WorkItems.Add(rejectedItem);

        var result = sprint.Complete();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.Should().Be(todoItem.Id);
    }

    [Fact]
    public void Complete_PlanningSprint_ReturnsFailure()
    {
        var sprint = SprintBuilder.New().Build();

        var result = sprint.Complete();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Active");
    }

    [Fact]
    public void Complete_CompletedSprint_ReturnsFailure()
    {
        var sprint = SprintBuilder.New().Completed().Build();

        var result = sprint.Complete();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Active");
    }

    [Theory]
    [InlineData(SprintStatus.Planning, true)]
    [InlineData(SprintStatus.Active, true)]
    [InlineData(SprintStatus.Completed, false)]
    public void CanAddItem_ReturnsExpectedResult(SprintStatus status, bool expected)
    {
        var sprint = SprintBuilder.New().WithStatus(status).Build();

        sprint.CanAddItem().Should().Be(expected);
    }

    [Fact]
    public void Sprint_DefaultsToPlanningStatus()
    {
        var sprint = SprintBuilder.New().Build();

        sprint.Status.Should().Be(SprintStatus.Planning);
    }

    [Fact]
    public void Sprint_HasUniqueId()
    {
        var sprint1 = SprintBuilder.New().Build();
        var sprint2 = SprintBuilder.New().Build();

        sprint1.Id.Should().NotBe(sprint2.Id);
        sprint1.Id.Should().NotBe(Guid.Empty);
    }
}
