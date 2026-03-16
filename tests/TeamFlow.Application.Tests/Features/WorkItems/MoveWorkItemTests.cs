using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.WorkItems.MoveWorkItem;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

[Collection("WorkItems")]
public sealed class MoveWorkItemTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_StoryMovedToNewEpic_Succeeds()
    {
        var project = await SeedProjectAsync();
        var story = await SeedWorkItemAsync(project.Id, b => b.WithType(WorkItemType.UserStory));
        var newEpic = await SeedWorkItemAsync(project.Id, b => b.AsEpic());

        var cmd = new MoveWorkItemCommand(story.Id, newEpic.Id);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.WorkItems.AsNoTracking().FirstAsync(w => w.Id == story.Id);
        updated.ParentId.Should().Be(newEpic.Id);
    }

    [Fact]
    public async Task Handle_InvalidReparent_ReturnsError()
    {
        var project = await SeedProjectAsync();
        var story = await SeedWorkItemAsync(project.Id, b => b.WithType(WorkItemType.UserStory));
        var anotherStory = await SeedWorkItemAsync(project.Id, b => b.WithType(WorkItemType.UserStory));

        // Story cannot be moved under another Story
        var cmd = new MoveWorkItemCommand(story.Id, anotherStory.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var cmd = new MoveWorkItemCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
