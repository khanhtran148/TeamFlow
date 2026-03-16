using FluentAssertions;
using TeamFlow.Application.Features.WorkItems.GetWorkItem;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

[Collection("WorkItems")]
public sealed class GetWorkItemTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ExistingItem_ReturnsDto()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.WithTitle("Test Item").WithType(WorkItemType.Task));

        var result = await Sender.Send(new GetWorkItemQuery(item.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Test Item");
        result.Value.Type.Should().Be(WorkItemType.Task);
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var result = await Sender.Send(new GetWorkItemQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_AssignedItem_ReturnsAssignedAtInDto()
    {
        var project = await SeedProjectAsync();
        var assignedAt = new DateTime(2026, 3, 15, 9, 23, 11, DateTimeKind.Utc);

        var assignee = UserBuilder.New().Build();
        DbContext.Users.Add(assignee);
        await DbContext.SaveChangesAsync();

        var item = await SeedWorkItemAsync(project.Id,
            b => b.WithType(WorkItemType.Task).WithAssignee(assignee.Id).WithAssignedAt(assignedAt));

        var result = await Sender.Send(new GetWorkItemQuery(item.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.AssignedAt.Should().Be(assignedAt);
    }

    [Fact]
    public async Task Handle_UnassignedItem_ReturnsNullAssignedAtInDto()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.WithType(WorkItemType.Task));

        var result = await Sender.Send(new GetWorkItemQuery(item.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.AssignedAt.Should().BeNull();
    }
}
