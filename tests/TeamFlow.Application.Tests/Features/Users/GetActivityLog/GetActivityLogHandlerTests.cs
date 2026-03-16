using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Users;
using TeamFlow.Application.Features.Users.GetActivityLog;

namespace TeamFlow.Application.Tests.Features.Users.GetActivityLog;

public sealed class GetActivityLogHandlerTests
{
    private readonly IActivityLogRepository _activityLogRepo = Substitute.For<IActivityLogRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid UserId = Guid.NewGuid();

    public GetActivityLogHandlerTests()
    {
        _currentUser.Id.Returns(UserId);
    }

    private GetActivityLogHandler CreateHandler() =>
        new(_activityLogRepo, _currentUser);

    [Fact]
    public async Task Handle_UserWithActivity_ReturnsPaginatedResults()
    {
        var items = Enumerable.Range(1, 5).Select(i => new ActivityLogItemDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            $"Work Item {i}",
            "StatusChanged",
            "Status",
            "ToDo",
            "InProgress",
            DateTime.UtcNow.AddMinutes(-i)
        )).ToList();

        _activityLogRepo.GetPagedByUserAsync(UserId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((items, 5));

        var result = await CreateHandler().Handle(new GetActivityLogQuery(1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(5);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_UserWithNoActivity_ReturnsEmptyPage()
    {
        _activityLogRepo.GetPagedByUserAsync(UserId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<ActivityLogItemDto>(), 0));

        var result = await CreateHandler().Handle(new GetActivityLogQuery(1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Page2_ReturnsCorrectOffset()
    {
        var items = Enumerable.Range(1, 3).Select(i => new ActivityLogItemDto(
            Guid.NewGuid(), Guid.NewGuid(), $"Item {i}", "Created", null, null, null, DateTime.UtcNow
        )).ToList();

        _activityLogRepo.GetPagedByUserAsync(UserId, 2, 20, Arg.Any<CancellationToken>())
            .Returns((items, 23));

        var result = await CreateHandler().Handle(new GetActivityLogQuery(2, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(2);
        result.Value.TotalCount.Should().Be(23);
        result.Value.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DefaultPageSize_Returns20Items()
    {
        _activityLogRepo.GetPagedByUserAsync(UserId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<ActivityLogItemDto>(), 0));

        var result = await CreateHandler().Handle(new GetActivityLogQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().Be(20);
        await _activityLogRepo.Received(1).GetPagedByUserAsync(UserId, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PageSizeAbove50_IsCappedAt50()
    {
        _activityLogRepo.GetPagedByUserAsync(UserId, 1, 50, Arg.Any<CancellationToken>())
            .Returns((new List<ActivityLogItemDto>(), 0));

        var result = await CreateHandler().Handle(new GetActivityLogQuery(1, 100), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().Be(50);
        await _activityLogRepo.Received(1).GetPagedByUserAsync(UserId, 1, 50, Arg.Any<CancellationToken>());
    }
}
