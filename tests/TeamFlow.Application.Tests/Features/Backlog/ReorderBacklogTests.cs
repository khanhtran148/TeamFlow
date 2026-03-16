using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Backlog.ReorderBacklog;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Backlog;

[Collection("WorkItems")]
public sealed class ReorderBacklogTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidReorder_UpdatesSortOrders()
    {
        var project = await SeedProjectAsync();
        var wi1 = await SeedWorkItemAsync(project.Id, b => b.AsTask());
        var wi2 = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        var items = new[]
        {
            new WorkItemSortOrder(wi1.Id, 10),
            new WorkItemSortOrder(wi2.Id, 20)
        };

        var result = await Sender.Send(new ReorderBacklogCommand(project.Id, items));

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var updated1 = await DbContext.WorkItems.AsNoTracking().FirstAsync(w => w.Id == wi1.Id);
        var updated2 = await DbContext.WorkItems.AsNoTracking().FirstAsync(w => w.Id == wi2.Id);
        updated1.SortOrder.Should().Be(10);
        updated2.SortOrder.Should().Be(20);
    }
}
