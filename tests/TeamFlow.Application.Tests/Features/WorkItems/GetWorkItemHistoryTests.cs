using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.WorkItems.GetHistory;

namespace TeamFlow.Application.Tests.Features.WorkItems;

public sealed class GetWorkItemHistoryTests
{
    private readonly IWorkItemHistoryRepository _historyRepo = Substitute.For<IWorkItemHistoryRepository>();

    private GetWorkItemHistoryHandler CreateHandler() => new(_historyRepo);

    [Fact]
    public async Task Handle_ValidQuery_ReturnsPagedHistory()
    {
        var workItemId = Guid.NewGuid();
        var historyItems = new[]
        {
            new WorkItemHistoryDto(Guid.NewGuid(), Guid.NewGuid(), "John", "User", "StatusChanged", "Status", "ToDo", "InProgress", DateTime.UtcNow)
        };
        _historyRepo.GetByWorkItemAsync(workItemId, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<WorkItemHistoryDto>(historyItems, 1, 1, 20));

        var result = await CreateHandler().Handle(
            new GetWorkItemHistoryQuery(workItemId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_NoHistory_ReturnsEmptyPage()
    {
        var workItemId = Guid.NewGuid();
        _historyRepo.GetByWorkItemAsync(workItemId, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<WorkItemHistoryDto>([], 0, 1, 20));

        var result = await CreateHandler().Handle(
            new GetWorkItemHistoryQuery(workItemId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
    }
}
