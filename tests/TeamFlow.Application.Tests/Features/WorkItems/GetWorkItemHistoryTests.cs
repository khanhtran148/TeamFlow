using FluentAssertions;
using TeamFlow.Application.Features.WorkItems.ChangeStatus;
using TeamFlow.Application.Features.WorkItems.GetHistory;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

[Collection("WorkItems")]
public sealed class GetWorkItemHistoryTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidQuery_ReturnsPagedHistory()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.WithStatus(WorkItemStatus.ToDo));

        // Generate some history by changing status
        await Sender.Send(new ChangeWorkItemStatusCommand(item.Id, WorkItemStatus.InProgress));
        DbContext.ChangeTracker.Clear();

        var result = await Sender.Send(new GetWorkItemHistoryQuery(item.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().BeGreaterThan(0);
        result.Value.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_NoHistory_ReturnsEmptyPage()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        var result = await Sender.Send(new GetWorkItemHistoryQuery(item.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }
}
