using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.WorkItems.DeleteWorkItem;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

[Collection("WorkItems")]
public sealed class DeleteWorkItemTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ExistingItem_SoftDeletes()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        var result = await Sender.Send(new DeleteWorkItemCommand(item.Id));

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        // Soft-deleted item should not appear via normal query (has query filter for DeletedAt)
        var found = await DbContext.WorkItems.AsNoTracking().FirstOrDefaultAsync(w => w.Id == item.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var result = await Sender.Send(new DeleteWorkItemCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_DeleteEpic_RecordsHistoryForEachDeleted()
    {
        var project = await SeedProjectAsync();
        // Create epic then a child story
        var epic = await SeedWorkItemAsync(project.Id, b => b.AsEpic());
        var story = await SeedWorkItemAsync(project.Id, b => b.WithType(WorkItemType.UserStory).WithParent(epic.Id));

        await Sender.Send(new DeleteWorkItemCommand(epic.Id));

        DbContext.ChangeTracker.Clear();
        var historyEntries = await DbContext.Set<Domain.Entities.WorkItemHistory>()
            .Where(h => (h.WorkItemId == epic.Id || h.WorkItemId == story.Id) && h.ActionType == "Deleted")
            .ToListAsync();
        historyEntries.Should().HaveCount(2);
    }
}
