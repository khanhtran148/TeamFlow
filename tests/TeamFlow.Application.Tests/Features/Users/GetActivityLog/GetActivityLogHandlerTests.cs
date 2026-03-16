using FluentAssertions;
using TeamFlow.Application.Features.Users.GetActivityLog;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;

namespace TeamFlow.Application.Tests.Features.Users.GetActivityLog;

[Collection("Auth")]
public sealed class GetActivityLogHandlerTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private async Task<WorkItem> SeedWorkItemAsync()
    {
        var project = await SeedProjectAsync();
        return await SeedWorkItemAsync(project.Id);
    }

    private async Task SeedHistoryAsync(Guid workItemId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            DbContext.Set<WorkItemHistory>().Add(new WorkItemHistory
            {
                WorkItemId = workItemId,
                ActorId = SeedUserId,
                ActorType = "User",
                ActionType = "StatusChanged",
                FieldName = "Status",
                OldValue = "ToDo",
                NewValue = "InProgress"
            });
        }

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_UserWithActivity_ReturnsPaginatedResults()
    {
        var workItem = await SeedWorkItemAsync();
        await SeedHistoryAsync(workItem.Id, 5);

        var result = await Sender.Send(new GetActivityLogQuery(1, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(5);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_UserWithNoActivity_ReturnsEmptyPage()
    {
        var result = await Sender.Send(new GetActivityLogQuery(1, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Page2_ReturnsCorrectOffset()
    {
        var workItem = await SeedWorkItemAsync();
        await SeedHistoryAsync(workItem.Id, 23);

        var result = await Sender.Send(new GetActivityLogQuery(2, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(2);
        result.Value.TotalCount.Should().Be(23);
        result.Value.Items.Should().HaveCount(3); // 23 - 20 = 3 on page 2
        result.Value.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DefaultPageSize_Returns20Items()
    {
        var result = await Sender.Send(new GetActivityLogQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_PageSizeAbove50_IsCappedAt50()
    {
        var result = await Sender.Send(new GetActivityLogQuery(1, 100));

        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().Be(50);
    }
}
