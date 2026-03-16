using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Search.ListSavedFilters;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Search;

public sealed class ListSavedFiltersTests
{
    private readonly ISavedFilterRepository _repo = Substitute.For<ISavedFilterRepository>();
    private readonly IPermissionChecker _permissionChecker = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public ListSavedFiltersTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissionChecker.HasPermissionAsync(ActorId, ProjectId, Permission.WorkItem_View, Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private ListSavedFiltersHandler CreateHandler() => new(_repo, _permissionChecker, _currentUser);

    [Fact]
    public async Task Handle_UserHasSavedFilters_ReturnsFilterList()
    {
        var filters = new List<SavedFilter>
        {
            new() { UserId = ActorId, ProjectId = ProjectId, Name = "My Bugs", FilterJson = JsonDocument.Parse("""{"type":"Bug"}"""), IsDefault = false },
            new() { UserId = ActorId, ProjectId = ProjectId, Name = "Sprint Tasks", FilterJson = JsonDocument.Parse("""{"sprint":"current"}"""), IsDefault = true }
        };
        _repo.ListByUserAndProjectAsync(ActorId, ProjectId, Arg.Any<CancellationToken>()).Returns(filters);

        var result = await CreateHandler().Handle(new ListSavedFiltersQuery(ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Name.Should().Be("My Bugs");
        result.Value[1].Name.Should().Be("Sprint Tasks");
        result.Value[1].IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoSavedFilters_ReturnsEmptyList()
    {
        _repo.ListByUserAndProjectAsync(ActorId, ProjectId, Arg.Any<CancellationToken>())
            .Returns(new List<SavedFilter>());

        var result = await CreateHandler().Handle(new ListSavedFiltersQuery(ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NotProjectMember_ReturnsForbidden()
    {
        _permissionChecker.HasPermissionAsync(ActorId, ProjectId, Permission.WorkItem_View, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(new ListSavedFiltersQuery(ProjectId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
