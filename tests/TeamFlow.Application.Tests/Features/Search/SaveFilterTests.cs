using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Search.SaveFilter;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Search;

public sealed class SaveFilterTests
{
    private readonly ISavedFilterRepository _repo = Substitute.For<ISavedFilterRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public SaveFilterTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _repo.AddAsync(Arg.Any<SavedFilter>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<SavedFilter>());
    }

    private SaveFilterHandler CreateHandler() => new(_repo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ValidCommand_CreatesFilter()
    {
        _repo.ExistsByNameAsync(ActorId, ProjectId, "My Filter", Arg.Any<CancellationToken>())
            .Returns(false);

        var filterJson = JsonDocument.Parse("""{"status":["ToDo"]}""");
        var cmd = new SaveFilterCommand(ProjectId, "My Filter", filterJson, false);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("My Filter");
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsConflict()
    {
        _repo.ExistsByNameAsync(ActorId, ProjectId, "Existing", Arg.Any<CancellationToken>())
            .Returns(true);

        var cmd = new SaveFilterCommand(ProjectId, "Existing", JsonDocument.Parse("{}"), false);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var cmd = new SaveFilterCommand(ProjectId, "Filter", JsonDocument.Parse("{}"), false);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
