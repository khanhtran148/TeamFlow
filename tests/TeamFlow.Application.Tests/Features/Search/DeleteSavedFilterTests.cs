using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Search.DeleteSavedFilter;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Search;

public sealed class DeleteSavedFilterTests
{
    private readonly ISavedFilterRepository _repo = Substitute.For<ISavedFilterRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public DeleteSavedFilterTests()
    {
        _currentUser.Id.Returns(ActorId);
    }

    private DeleteSavedFilterHandler CreateHandler() => new(_repo, _currentUser);

    [Fact]
    public async Task Handle_OwnFilter_DeletesSuccessfully()
    {
        var filter = new SavedFilter { UserId = ActorId, ProjectId = ProjectId, Name = "Test" };
        _repo.GetByIdAsync(filter.Id, Arg.Any<CancellationToken>()).Returns(filter);

        var result = await CreateHandler().Handle(new DeleteSavedFilterCommand(ProjectId, filter.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).DeleteAsync(filter.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OtherUsersFilter_ReturnsForbidden()
    {
        var otherUserId = Guid.NewGuid();
        var filter = new SavedFilter { UserId = otherUserId, ProjectId = ProjectId, Name = "Test" };
        _repo.GetByIdAsync(filter.Id, Arg.Any<CancellationToken>()).Returns(filter);

        var result = await CreateHandler().Handle(new DeleteSavedFilterCommand(ProjectId, filter.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_FilterNotFound_ReturnsNotFound()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SavedFilter?)null);

        var result = await CreateHandler().Handle(new DeleteSavedFilterCommand(ProjectId, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
