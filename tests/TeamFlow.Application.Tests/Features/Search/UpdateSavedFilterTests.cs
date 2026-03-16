using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Search.UpdateSavedFilter;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.Search;

public sealed class UpdateSavedFilterTests
{
    private readonly ISavedFilterRepository _repo = Substitute.For<ISavedFilterRepository>();
    private readonly IPermissionChecker _permissionChecker = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public UpdateSavedFilterTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissionChecker.HasPermissionAsync(ActorId, ProjectId, Permission.WorkItem_View, Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private UpdateSavedFilterHandler CreateHandler() => new(_repo, _permissionChecker, _currentUser);

    private SavedFilter CreateOwnedFilter(string name = "Original") => new()
    {
        UserId = ActorId,
        ProjectId = ProjectId,
        Name = name,
        FilterJson = JsonDocument.Parse("""{"status":"Open"}"""),
        IsDefault = false
    };

    [Fact]
    public async Task Handle_UpdateName_ReturnsUpdatedFilter()
    {
        var filter = CreateOwnedFilter();
        _repo.GetByIdAsync(filter.Id, Arg.Any<CancellationToken>()).Returns(filter);
        _repo.ExistsByNameAsync(ActorId, ProjectId, "Renamed", Arg.Any<CancellationToken>()).Returns(false);

        var cmd = new UpdateSavedFilterCommand(ProjectId, filter.Id, "Renamed", null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Renamed");
        await _repo.Received(1).UpdateAsync(Arg.Is<SavedFilter>(f => f.Name == "Renamed"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdateFilterJson_ReturnsUpdatedFilter()
    {
        var filter = CreateOwnedFilter();
        _repo.GetByIdAsync(filter.Id, Arg.Any<CancellationToken>()).Returns(filter);
        var newJson = JsonDocument.Parse("""{"status":"Closed"}""");

        var cmd = new UpdateSavedFilterCommand(ProjectId, filter.Id, null, newJson, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).UpdateAsync(Arg.Is<SavedFilter>(f => f.FilterJson == newJson), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FilterNotFound_ReturnsNotFound()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SavedFilter?)null);

        var cmd = new UpdateSavedFilterCommand(ProjectId, Guid.NewGuid(), "New Name", null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsAccessDenied()
    {
        var otherUserId = Guid.NewGuid();
        var filter = new SavedFilter
        {
            UserId = otherUserId,
            ProjectId = ProjectId,
            Name = "Other's Filter"
        };
        _repo.GetByIdAsync(filter.Id, Arg.Any<CancellationToken>()).Returns(filter);

        var cmd = new UpdateSavedFilterCommand(ProjectId, filter.Id, "Stolen", null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_NotProjectMember_ReturnsForbidden()
    {
        _permissionChecker.HasPermissionAsync(ActorId, ProjectId, Permission.WorkItem_View, Arg.Any<CancellationToken>())
            .Returns(false);

        var cmd = new UpdateSavedFilterCommand(ProjectId, Guid.NewGuid(), "Name", null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsConflict()
    {
        var filter = CreateOwnedFilter();
        _repo.GetByIdAsync(filter.Id, Arg.Any<CancellationToken>()).Returns(filter);
        _repo.ExistsByNameAsync(ActorId, ProjectId, "Duplicate", Arg.Any<CancellationToken>()).Returns(true);

        var cmd = new UpdateSavedFilterCommand(ProjectId, filter.Id, "Duplicate", null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }
}
