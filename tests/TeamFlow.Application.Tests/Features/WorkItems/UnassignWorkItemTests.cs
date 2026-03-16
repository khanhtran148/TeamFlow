using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.WorkItems.UnassignWorkItem;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

[Collection("WorkItems")]
public sealed class UnassignWorkItemTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_AssignedItem_ClearsAssignee()
    {
        var project = await SeedProjectAsync();
        var assignee = UserBuilder.New().Build();
        DbContext.Users.Add(assignee);
        await DbContext.SaveChangesAsync();

        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask().WithAssignee(assignee.Id));

        var result = await Sender.Send(new UnassignWorkItemCommand(item.Id));

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.WorkItems.AsNoTracking().FirstAsync(w => w.Id == item.Id);
        updated.AssigneeId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UnassignedItem_NoError()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        var result = await Sender.Send(new UnassignWorkItemCommand(item.Id));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var result = await Sender.Send(new UnassignWorkItemCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AssignedItem_ClearsAssignedAt()
    {
        var project = await SeedProjectAsync();
        var assignee = UserBuilder.New().Build();
        DbContext.Users.Add(assignee);
        await DbContext.SaveChangesAsync();

        var item = await SeedWorkItemAsync(project.Id,
            b => b.AsTask().WithAssignee(assignee.Id).WithAssignedAt(DateTime.UtcNow.AddDays(-3)));

        await Sender.Send(new UnassignWorkItemCommand(item.Id));

        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.WorkItems.AsNoTracking().FirstAsync(w => w.Id == item.Id);
        updated.AssignedAt.Should().BeNull();
    }
}
